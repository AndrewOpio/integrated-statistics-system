using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.CfbReports;
using VMSService.Core.Repositories.CfbReports;
using VMSService.Core.Repositories.CfbReportStatusHistories;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CfbReportsController : Controller
    {
        private readonly ICfbReportRepository _cfbReportRepository;
        private readonly ICfbReportStatusHistoryRepository _statusHistoryRepository;
        private readonly IWorkflowServiceClient _workflowServiceClient;
        private readonly IWorkflowService _workflowService;
        private readonly IConfigurationsServiceClient _configServiceClient;

        public CfbReportsController(
            ICfbReportRepository cfbReportRepository,
            ICfbReportStatusHistoryRepository statusHistoryRepository,
            IWorkflowServiceClient workflowServiceClient,
            IWorkflowService workflowService,
            IConfigurationsServiceClient configServiceClient)
        {
            _cfbReportRepository     = cfbReportRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _workflowServiceClient   = workflowServiceClient;
            _workflowService         = workflowService;
            _configServiceClient     = configServiceClient;
        }

        [Breadcrumb("CFB Reports", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var reports = await _cfbReportRepository.FindAll();
            return View(reports);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(CfbReportsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionPreview(int branchId, DateTime periodStart, DateTime periodEnd)
        {
            if (branchId <= 0 || periodStart == default || periodEnd == default || periodEnd < periodStart)
                return Ok(new { transactions = Array.Empty<object>() });

            var transactions = await _cfbReportRepository.GetUnlinkedTransactions(branchId, periodStart, periodEnd);

            var result = transactions.Select(t => new
            {
                t.TellerName,
                TransactionDate      = t.TransactionDate.ToString("dd/MMM/yyyy"),
                TransactionReference = t.TransactionReference ?? "–",
                t.Type,
                t.Amount
            });

            return Ok(new { transactions = result });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCfbReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                int initialStatusId      = 0;
                string initialStatusName = StatusConstants.CfbReport.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.CfbReport,
                        StatusName         = StatusConstants.CfbReport.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                var report = new CfbReport
                {
                    ReferenceNumber   = dto.ReferenceNumber,
                    PeriodStart       = dto.PeriodStart,
                    PeriodEnd         = dto.PeriodEnd,
                    BranchId          = dto.BranchId,
                    Remarks           = dto.Remarks,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                };

                await _cfbReportRepository.Create(report);

                // Link all unlinked transactions for this branch/period
                var unlinked = await _cfbReportRepository.GetUnlinkedTransactions(dto.BranchId, dto.PeriodStart, dto.PeriodEnd);
                if (unlinked.Any())
                    await _cfbReportRepository.LinkTransactionsToReport(report.Id, unlinked.Select(t => t.Id));

                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = report.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(CfbReportsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var report = await _cfbReportRepository.FindById(id.Value);
            if (report == null) return NotFound();

            if (report.CurrentStatusId == 0)
            {
                try
                {
                    var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.CfbReport,
                        StatusName         = report.CurrentStatusName,
                    });
                    if (status?.Id > 0)
                    {
                        report.CurrentStatusId = status.Id;
                        await _cfbReportRepository.Update(report);
                    }
                }
                catch { }
            }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.CfbReport,
                    CurrentStatusId    = report.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var branches   = await _configServiceClient.GetAllBouBranchesAsync();
            var branchName = branches?.FirstOrDefault(b => b.Id == report.BranchId)?.Name ?? report.BranchId.ToString();

            var historyEntities = await _statusHistoryRepository.FindByCfbReportId(id.Value);
            var statusHistory = historyEntities.Select(h => new CfbReportStatusHistoryViewModel
            {
                OldStatusId   = h.OldStatusId,
                OldStatusName = h.OldStatusName,
                NewStatusId   = h.NewStatusId,
                NewStatusName = h.NewStatusName,
                ChangedBy     = h.ChangedBy,
                ChangedAt     = h.ChangedAt,
                Remarks       = h.Remarks
            });

            var vm = new CfbReportDetailsViewModel
            {
                Id                   = report.Id,
                ReferenceNumber      = report.ReferenceNumber,
                PeriodStart          = report.PeriodStart,
                PeriodEnd            = report.PeriodEnd,
                BranchId             = report.BranchId,
                BranchName           = branchName,
                Remarks              = report.Remarks,
                CurrentStatusId      = report.CurrentStatusId,
                CurrentStatusName    = report.CurrentStatusName,
                CreatedBy            = report.CreatedBy,
                CreatedDate          = report.CreatedDate,
                Transactions         = report.CfbReportTransactions ?? [],
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
            };

            return View(vm);
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(CfbReportsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var report = await _cfbReportRepository.FindById(id.Value);
            if (report == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateCfbReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var report = await _cfbReportRepository.FindById(dto.Id);
                if (report == null) return NotFound();

                report.ReferenceNumber = dto.ReferenceNumber;
                report.PeriodStart     = dto.PeriodStart;
                report.PeriodEnd       = dto.PeriodEnd;
                report.BranchId        = dto.BranchId;
                report.Remarks         = dto.Remarks;

                await _cfbReportRepository.Update(report);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = dto.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var report = await _cfbReportRepository.FindById(id);
            if (report == null)
            {
                TempData["Error"] = "CFB Report not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.CfbReport,
                    entityId:        id,
                    currentStatusId: report.CurrentStatusId,
                    actionName:      actionName,
                    username:        username,
                    userRoles:       userRoles,
                    remarks:         remarks);

                TempData[executed ? "Success" : "Error"] = executed
                    ? "Action completed successfully."
                    : $"Failed to execute '{actionName}'. You may not have the required role or the action is unavailable.";
            }
            catch
            {
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
    }
}
