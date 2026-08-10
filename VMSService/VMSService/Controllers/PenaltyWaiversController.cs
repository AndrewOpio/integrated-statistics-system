using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class PenaltyWaiversController(
        IPenaltyWaiverRepository _penaltyWaiverRepository,
        IPenaltyRepository _penaltyRepository,
        IConfigurationsServiceClient _configServiceClient,
        ILogger<PenaltyWaiversController> _logger) : Controller
    {
        // ── Role helper ─────────────────────────────────────────────────────

        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private bool IsAdmin() => GetUserRoles().Contains(SystemPolicy.Roles.Admin);

        // ── Index ─────────────────────────────────────────────────────

        public async Task<IActionResult> Index(int? penaltyId)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var waivers = penaltyId.HasValue
                    ? await _penaltyWaiverRepository.FindByPenalty(penaltyId.Value)
                    : await _penaltyWaiverRepository.FindAll();

                ViewBag.PenaltyFilter = penaltyId;
                return View(waivers.Select(ToViewModel).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving penalty waivers");
                TempData["Error"] = "An error occurred while retrieving penalty waivers. Please try again.";
                return View(new List<PenaltyWaiverViewModel>());
            }
        }

        // ── Create ─────────────────────────────────────────────────────

        public async Task<IActionResult> Create(int? penaltyId)
        {
            if (!IsAdmin()) return Forbid();

            var model = new PenaltyWaiverViewModel
            {
                PenaltyId = penaltyId ?? 0,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
            };
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PenaltyWaiverViewModel model)
        {
            if (!IsAdmin()) return Forbid();

            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before the start date.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            try
            {
                var branch = (await _configServiceClient.GetAllBouBranchesAsync())
                    .FirstOrDefault(b => b.Id == model.BouBranchId);

                var waiver = new PenaltyWaiver
                {
                    PenaltyId = model.PenaltyId,
                    BouBranchId = model.BouBranchId,
                    BouBranchCode = branch?.BranchCode ?? string.Empty,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Reason = model.Reason,
                    Active = true,
                };

                await _penaltyWaiverRepository.Create(waiver);
                await _penaltyWaiverRepository.Save();

                TempData["Success"] = "Penalty waiver created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating penalty waiver");
                ModelState.AddModelError("", "Failed to create penalty waiver. An unexpected error occurred.");
            }

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // ── Edit ─────────────────────────────────────────────────────

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin()) return Forbid();
            if (id == null) return NotFound();

            var waiver = await _penaltyWaiverRepository.FindById(id.Value);
            if (waiver == null) return NotFound();

            var model = ToViewModel(waiver);
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PenaltyWaiverViewModel model)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return NotFound();

            if (model.EndDate < model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before the start date.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            try
            {
                var waiver = await _penaltyWaiverRepository.FindById(id);
                if (waiver == null) return NotFound();

                var branch = (await _configServiceClient.GetAllBouBranchesAsync())
                    .FirstOrDefault(b => b.Id == model.BouBranchId);

                waiver.PenaltyId = model.PenaltyId;
                waiver.BouBranchId = model.BouBranchId;
                waiver.BouBranchCode = branch?.BranchCode ?? waiver.BouBranchCode;
                waiver.StartDate = model.StartDate;
                waiver.EndDate = model.EndDate;
                waiver.Reason = model.Reason;
                waiver.Active = model.Active;

                await _penaltyWaiverRepository.Update(waiver);
                await _penaltyWaiverRepository.Save();

                TempData["Success"] = "Penalty waiver updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating penalty waiver with id {Id}", id);
                ModelState.AddModelError("", "Failed to update penalty waiver. An unexpected error occurred.");
            }

            await PopulateDropdownsAsync(model);
            return View(model);
        }

        // ── Revoke ─────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            if (!IsAdmin()) return Forbid();

            try
            {
                var waiver = await _penaltyWaiverRepository.FindById(id);
                if (waiver == null)
                {
                    TempData["Error"] = "Penalty waiver not found.";
                    return RedirectToAction(nameof(Index));
                }

                waiver.Active = false;
                await _penaltyWaiverRepository.Update(waiver);
                await _penaltyWaiverRepository.Save();

                TempData["Success"] = "Penalty waiver revoked.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking penalty waiver with id {Id}", id);
                TempData["Error"] = "An error occurred while revoking the waiver. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ───────────────────────────────────────────────────

        private async Task PopulateDropdownsAsync(PenaltyWaiverViewModel model)
        {
            var penalties = await _penaltyRepository.FindAll();
            model.Penalties = penalties
                .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
                .ToList();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BouBranches = branches
                .Select(b => new SelectListItem($"{b.Name} ({b.BranchCode})", b.Id.ToString()))
                .ToList();
        }

        private static PenaltyWaiverViewModel ToViewModel(PenaltyWaiver w) => new()
        {
            Id = w.Id,
            PenaltyId = w.PenaltyId,
            PenaltyName = w.Penalty?.Name ?? string.Empty,
            BouBranchId = w.BouBranchId,
            BouBranchCode = w.BouBranchCode,
            StartDate = w.StartDate,
            EndDate = w.EndDate,
            Reason = w.Reason,
            Active = w.Active,
            CreatedBy = w.CreatedBy,
            CreatedDate = w.CreatedDate,
        };
    }
}
