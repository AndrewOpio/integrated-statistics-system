using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.PublicHolidays;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class PublicHolidaysController(
        IPublicHolidayRepository _publicHolidayRepository,
        ILogger<PublicHolidaysController> _logger) : Controller
    {
        // ── Role helper ─────────────────────────────────────────────────────

        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private bool IsAdmin() => GetUserRoles().Contains(SystemPolicy.Roles.Admin);

        // ── Past-date lock ───────────────────────────────────────────
        // A holiday's Date/Name is historical fact once the date has passed — editing it
        // retroactively would misrepresent what was in effect when past requests were decided.
        // Activate/Deactivate remains open (see SetActive) since it's a non-destructive correction.
        private static readonly TimeZoneInfo EatZone = TimeZoneInfo.FindSystemTimeZoneById("E. Africa Standard Time");

        private static DateOnly TodayEat()
            => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EatZone));

        // ── Index ─────────────────────────────────────────────────────

        public async Task<IActionResult> Index(bool? active)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var holidays = active.HasValue
                    ? await _publicHolidayRepository.FindByActive(active.Value)
                    : await _publicHolidayRepository.FindAll();

                ViewBag.ActiveFilter = active;
                return View(holidays.Select(ToViewModel).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public holidays");
                TempData["Error"] = "An error occurred while retrieving public holidays. Please try again.";
                return View(new List<PublicHolidayViewModel>());
            }
        }

        // ── Details ─────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var holiday = await _publicHolidayRepository.FindById(id.Value);
            if (holiday == null) return NotFound();

            return View(ToViewModel(holiday));
        }

        // ── Create ─────────────────────────────────────────────────────

        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();

            return View(new PublicHolidayViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PublicHolidayViewModel model)
        {
            if (!IsAdmin()) return Forbid();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                return View(model);
            }

            try
            {
                var holiday = ToEntity(model);
                await _publicHolidayRepository.Create(holiday);
                await _publicHolidayRepository.Save();

                TempData["Success"] = "Public holiday created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating public holiday");
                ModelState.AddModelError("", "Failed to create public holiday. A holiday may already exist for that date.");
            }

            return View(model);
        }

        // ── Edit ─────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var holiday = await _publicHolidayRepository.FindById(id.Value);
            if (holiday == null) return NotFound();

            if (holiday.Date < TodayEat())
            {
                TempData["Error"] = "This holiday's date has already passed and can no longer be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(ToViewModel(holiday));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PublicHolidayViewModel model)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return NotFound();

            var holiday = await _publicHolidayRepository.FindById(id);
            if (holiday == null) return NotFound();

            if (holiday.Date < TodayEat())
            {
                TempData["Error"] = "This holiday's date has already passed and can no longer be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                return View(model);
            }

            try
            {
                ApplyToEntity(model, holiday);
                await _publicHolidayRepository.Update(holiday);
                await _publicHolidayRepository.Save();

                TempData["Success"] = "Public holiday updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating public holiday with id {Id}", id);
                ModelState.AddModelError("", "Failed to update public holiday. A holiday may already exist for that date.");
            }

            return View(model);
        }

        // ── Activate / Deactivate ────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            if (!IsAdmin()) return Forbid();
            return await SetActive(id, true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (!IsAdmin()) return Forbid();
            return await SetActive(id, false);
        }

        private async Task<IActionResult> SetActive(int id, bool active)
        {
            try
            {
                var holiday = await _publicHolidayRepository.FindById(id);
                if (holiday == null)
                {
                    TempData["Error"] = "Public holiday not found.";
                    return RedirectToAction(nameof(Index));
                }

                holiday.Active = active;
                await _publicHolidayRepository.Update(holiday);
                await _publicHolidayRepository.Save();

                TempData["Success"] = active ? "Public holiday activated." : "Public holiday deactivated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active state for public holiday with id {Id}", id);
                TempData["Error"] = "An error occurred while updating the public holiday. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Mapping helpers ───────────────────────────────────────────

        private static PublicHolidayViewModel ToViewModel(PublicHoliday h) => new()
        {
            Id = h.Id,
            Date = h.Date,
            Name = h.Name,
            Active = h.Active,
            IsPast = h.Date < TodayEat(),
        };

        private static PublicHoliday ToEntity(PublicHolidayViewModel m) => new()
        {
            Date = m.Date,
            Name = m.Name,
            Active = m.Active,
        };

        private static void ApplyToEntity(PublicHolidayViewModel m, PublicHoliday h)
        {
            h.Date = m.Date;
            h.Name = m.Name;
            h.Active = m.Active;
        }
    }
}
