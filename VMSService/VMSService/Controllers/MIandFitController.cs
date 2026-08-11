using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.MIandFits;
using VMSService.Core.Repositories.MIandFits;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Static;
using WorkflowService.Client;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class MIandFitController(
        IMIandFitRepository _miAndFitRepository,
        IStockProcessingPlanRepository _planRepository,
        IStockService _stockService,
        IWorkflowServiceClient _workflowServiceClient,
        ILogger<MIandFitController> _logger) : Controller
    {
        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        [HttpGet]
        [Breadcrumb("MI & Fit", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.BoardSupervisor))
                return Forbid();

            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

            try
            {
                var myMiFits = await _miAndFitRepository.FindByBoardSupervisor(username);

                var rows = new List<MIandFitPlanSummaryViewModel>();
                foreach (var group in myMiFits.GroupBy(m => m.StockProcessingPlanId))
                {
                    var plan = group.First().StockProcessingPlan;
                    if (plan == null) continue;

                    var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);

                    // Supervisors may only declare while the plan is Issued To Boards — before that
                    // the maker is still editing, and after that the plan has moved past this stage.
                    if (statusType == null || statusType.Name != StatusConstants.StockProcessingPlan.IssuedToBoards)
                        continue;

                    rows.Add(new MIandFitPlanSummaryViewModel
                    {
                        StockProcessingPlanId = plan.Id,
                        PlanReferenceNumber = plan.ReferenceNumber,
                        ProcessingDate = plan.ProcessingDate,
                        StatusName = statusType.Name,
                        DenominationCount = group.Count(),
                        PendingCount = group.Count(m => m.ReturnedFitValue == 0 && m.ReturnedUnFitValue == 0)
                    });
                }

                return View(rows.OrderByDescending(r => r.ProcessingDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving MI & Fit assignments for {Username}", username);
                TempData["Error"] = "An error occurred while retrieving your MI & Fit assignments.";
                return View(new List<MIandFitPlanSummaryViewModel>());
            }
        }

        [HttpGet]
        [Breadcrumb("Declare", FromController = typeof(MIandFitController), FromAction = nameof(Index))]
        public async Task<IActionResult> Declare(int id)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.BoardSupervisor))
                return Forbid();

            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);
                if (statusType == null || statusType.Name != StatusConstants.StockProcessingPlan.IssuedToBoards)
                {
                    TempData["Error"] = "MI & Fit can only be declared while the plan is Issued To Boards.";
                    return RedirectToAction(nameof(Index));
                }

                var myRowsForPlan = (await _miAndFitRepository.FindByPlanId(id))
                    .Where(m => m.BoardSupervisorUserId == username)
                    .ToList();

                if (myRowsForPlan.Count == 0)
                {
                    TempData["Error"] = "No MI & Fit denominations are assigned to you for this plan.";
                    return RedirectToAction(nameof(Index));
                }

                var denominations = await _stockService.GetAllDenominations();
                var denomDict = denominations.ToDictionary(d => d.Id);

                var model = new MIandFitDeclareViewModel
                {
                    StockProcessingPlanId = plan.Id,
                    PlanReferenceNumber = plan.ReferenceNumber,
                    ProcessingDate = plan.ProcessingDate,
                    Rows = myRowsForPlan
                        .Select(m =>
                        {
                            var denom = denomDict.GetValueOrDefault(m.DenominationId);
                            return new MIandFitViewModel
                            {
                                Id = m.Id,
                                StockProcessingPlanId = m.StockProcessingPlanId,
                                DenominationId = m.DenominationId,
                                DenominationName = denom?.Name,
                                DenominationValue = denom?.Value ?? 0,
                                IssuedFitValue = m.IssuedFitValue,
                                IssuedUnFitValue = m.IssuedUnFitValue,
                                ReturnedFitValue = m.ReturnedFitValue,
                                ReturnedUnFitValue = m.ReturnedUnFitValue,
                                BoardSupervisorUserId = m.BoardSupervisorUserId
                            };
                        })
                        .OrderBy(r => r.DenominationValue)
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading MI & Fit declare form for plan {Id}", id);
                TempData["Error"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Declare(int id, string? returnsJson)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.BoardSupervisor))
                return Forbid();

            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

            try
            {
                var plan = await _planRepository.FindById(id);
                if (plan == null) return NotFound();

                var statusType = await _workflowServiceClient.FindStatusById(plan.CurrentStatusId);
                if (statusType == null || statusType.Name != StatusConstants.StockProcessingPlan.IssuedToBoards)
                {
                    TempData["Error"] = "MI & Fit can only be declared while the plan is Issued To Boards.";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(returnsJson) || returnsJson == "[]")
                {
                    TempData["Error"] = "No values were submitted.";
                    return RedirectToAction(nameof(Declare), new { id });
                }

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var entries = System.Text.Json.JsonSerializer.Deserialize<List<MIandFitReturnEntry>>(returnsJson, options) ?? [];

                var myRows = (await _miAndFitRepository.FindByPlanId(id))
                    .Where(m => m.BoardSupervisorUserId == username)
                    .ToDictionary(m => m.Id);

                foreach (var entry in entries)
                {
                    // Every posted row must belong to this supervisor AND this plan.
                    if (!myRows.TryGetValue(entry.Id, out var row) || row.StockProcessingPlanId != id)
                        return Forbid();

                    row.ReturnedFitValue = entry.ReturnedFitValue;
                    row.ReturnedUnFitValue = entry.ReturnedUnFitValue;
                }

                await _miAndFitRepository.SaveAll();

                TempData["Success"] = "MI & Fit values submitted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving MI & Fit declarations for plan {Id}", id);
                TempData["Error"] = "An error occurred while saving your submission.";
                return RedirectToAction(nameof(Declare), new { id });
            }
        }
    }
}
