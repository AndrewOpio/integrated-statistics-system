using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class PenaltiesController(
        IPenaltyRepository _penaltyRepository,
        ILogger<PenaltiesController> _logger) : Controller
    {
        // ── Role helper ─────────────────────────────────────────────────────

        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private bool IsAdmin() => GetUserRoles().Contains(SystemPolicy.Roles.Admin);

        // ── Index ─────────────────────────────────────────────────────

        public async Task<IActionResult> Index(bool? active)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var penalties = active.HasValue
                    ? await _penaltyRepository.FindByActive(active.Value)
                    : await _penaltyRepository.FindAll();

                ViewBag.ActiveFilter = active;
                return View(penalties.Select(ToViewModel).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving penalties");
                TempData["Error"] = "An error occurred while retrieving penalties. Please try again.";
                return View(new List<PenaltyViewModel>());
            }
        }

        // ── Details ─────────────────────────────────────────────────────

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var penalty = await _penaltyRepository.FindById(id.Value);
            if (penalty == null) return NotFound();

            return View(ToViewModel(penalty));
        }

        // ── Create ─────────────────────────────────────────────────────

        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();

            var model = new PenaltyViewModel { AppliesToList = GetAppliesToList() };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PenaltyViewModel model)
        {
            if (!IsAdmin()) return Forbid();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                model.AppliesToList = GetAppliesToList();
                return View(model);
            }

            try
            {
                var penalty = ToEntity(model);
                await _penaltyRepository.Create(penalty);
                await _penaltyRepository.Save();

                TempData["Success"] = "Penalty created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating penalty");
                ModelState.AddModelError("", "Failed to create penalty. An unexpected error occurred.");
            }

            model.AppliesToList = GetAppliesToList();
            return View(model);
        }

        // ── Edit ─────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var penalty = await _penaltyRepository.FindById(id.Value);
            if (penalty == null) return NotFound();

            var model = ToViewModel(penalty);
            model.AppliesToList = GetAppliesToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PenaltyViewModel model)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                model.AppliesToList = GetAppliesToList();
                return View(model);
            }

            try
            {
                var penalty = await _penaltyRepository.FindById(id);
                if (penalty == null) return NotFound();

                ApplyToEntity(model, penalty);
                await _penaltyRepository.Update(penalty);
                await _penaltyRepository.Save();

                TempData["Success"] = "Penalty updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating penalty with id {Id}", id);
                ModelState.AddModelError("", "Failed to update penalty. An unexpected error occurred.");
            }

            model.AppliesToList = GetAppliesToList();
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
                var penalty = await _penaltyRepository.FindById(id);
                if (penalty == null)
                {
                    TempData["Error"] = "Penalty not found.";
                    return RedirectToAction(nameof(Index));
                }

                penalty.Active = active;
                await _penaltyRepository.Update(penalty);
                await _penaltyRepository.Save();

                TempData["Success"] = active ? "Penalty activated." : "Penalty deactivated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active state for penalty with id {Id}", id);
                TempData["Error"] = "An error occurred while updating the penalty. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Mapping helpers ───────────────────────────────────────────

        private static List<SelectListItem> GetAppliesToList() =>
        [
            new(PenaltyConstants.AppliesTo.PreTransaction, PenaltyConstants.AppliesTo.PreTransaction),
            new(PenaltyConstants.AppliesTo.PointOfTransaction, PenaltyConstants.AppliesTo.PointOfTransaction),
            new(PenaltyConstants.AppliesTo.Processing, PenaltyConstants.AppliesTo.Processing)
        ];

        private static PenaltyViewModel ToViewModel(Penalty p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            AppliesTo = p.AppliesTo,
            Unit = p.Unit,
            CutOffTime = p.CutOffTime,
            PenaltyValue = p.PenaltyValue,
            PenaltyPercent = p.PenaltyPercent,
            PercentCutOff = p.PercentCutOff,
            PenaltyMultiplier = p.PenaltyMultiplier,
            Active = p.Active
        };

        private static Penalty ToEntity(PenaltyViewModel m) => new()
        {
            Name = m.Name,
            Description = m.Description,
            AppliesTo = m.AppliesTo,
            Unit = m.Unit,
            CutOffTime = m.CutOffTime,
            PenaltyValue = m.PenaltyValue,
            PenaltyPercent = m.PenaltyPercent,
            PercentCutOff = m.PercentCutOff,
            PenaltyMultiplier = m.PenaltyMultiplier,
            Active = m.Active
        };

        private static void ApplyToEntity(PenaltyViewModel m, Penalty p)
        {
            p.Name = m.Name;
            p.Description = m.Description;
            p.AppliesTo = m.AppliesTo;
            p.Unit = m.Unit;
            p.CutOffTime = m.CutOffTime;
            p.PenaltyValue = m.PenaltyValue;
            p.PenaltyPercent = m.PenaltyPercent;
            p.PercentCutOff = m.PercentCutOff;
            p.PenaltyMultiplier = m.PenaltyMultiplier;
            p.Active = m.Active;
        }
    }
}
