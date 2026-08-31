using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.Penalties;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Static;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class AppliedPenaltiesController(
        IAppliedPenaltyRepository _appliedPenaltyRepository,
        ILogger<AppliedPenaltiesController> _logger) : Controller
    {
        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private bool CanView()
        {
            var roles = GetUserRoles();
            return roles.Contains(SystemPolicy.Roles.BackOffice) || roles.Contains(SystemPolicy.Roles.RemittancesOfficer);
        }

        // Penalties applied against a request but not yet settled on RTGS — a worklist view
        // for Back Office / Remittance, spanning all sources (withdrawal, deposit, processing).
        public async Task<IActionResult> Index()
        {
            if (!CanView()) return Forbid();

            try
            {
                var unsettled = await _appliedPenaltyRepository.FindUnsettled();

                var model = unsettled.Select(a => new AppliedPenaltyViewModel
                {
                    Id = a.Id,
                    Source = a.Source,
                    SourceId = a.SourceId,
                    PenaltyId = a.PenaltyId,
                    PenaltyName = a.Penalty?.Name ?? string.Empty,
                    ChargeValue = a.ChargeValue,
                    Quantity = a.Quantity,
                    IsSettledOnRtgs = a.IsSettledOnRtgs,
                    RtgsCode = a.RtgsCode,
                    RtgsSettlingMessage = a.RtgsSettlingMessage,
                    AppliedBy = a.CreatedBy,
                    AppliedAt = a.CreatedDate,
                    ToBeWaived = a.ToBeWaived,
                    WaiverReason = a.WaiverReason,
                    WaivedBy = a.WaivedBy,
                    WaivedDate = a.WaivedDate,
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unsettled applied penalties");
                TempData["Error"] = "An error occurred while retrieving unsettled penalties. Please try again.";
                return View(new List<AppliedPenaltyViewModel>());
            }
        }
    }
}
