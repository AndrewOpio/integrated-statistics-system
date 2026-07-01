using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.EodDiscrepancyReports;
using VMSService.Core.Repositories.EodDiscrepancyReports;
using VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class EndOfDayDiscrepanciesController : Controller
    {
        private readonly IEodDiscrepancyReportRepository _cfbReportRepository;
        private readonly IEodDiscrepancyReportStatusHistoryRepository _statusHistoryRepository;
        private readonly IWorkflowServiceClient _workflowServiceClient;
        private readonly IWorkflowService _workflowService;
        private readonly IConfigurationsServiceClient _configServiceClient;
        private readonly IDenominationRepository _denominationRepository;

        public EndOfDayDiscrepanciesController(
            IEodDiscrepancyReportRepository cfbReportRepository,
            IEodDiscrepancyReportStatusHistoryRepository statusHistoryRepository,
            IWorkflowServiceClient workflowServiceClient,
            IWorkflowService workflowService,
            IConfigurationsServiceClient configServiceClient,
            IDenominationRepository denominationRepository)
        {
            _cfbReportRepository     = cfbReportRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _workflowServiceClient   = workflowServiceClient;
            _workflowService         = workflowService;
            _configServiceClient     = configServiceClient;
            _denominationRepository  = denominationRepository;
        }

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.View)]
        [Breadcrumb("End of Day Discrepancies", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var reports = await _cfbReportRepository.FindAll();
            return View(reports);
        }

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.Create)]
        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(EndOfDayDiscrepanciesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches      = await _configServiceClient.GetAllBouBranchesAsync();
            var denominations = await _denominationRepository.FindAll();
            var banks         = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.Branches      = branches?.ToList() ?? new();
            ViewBag.Denominations = denominations?.Where(d => !d.IsDeleted).ToList() ?? new();
            ViewBag.Banks         = banks?.ToList() ?? new();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionPreview(int branchId, DateTime sessionDate)
        {
            if (branchId <= 0 || sessionDate == default)
                return Ok(new { transactions = Array.Empty<object>() });

            var transactions = await _cfbReportRepository.GetUnlinkedTransactions(branchId, sessionDate);

            var result = transactions.Select(t => new
            {
                SessionDate          = t.CreatedDate.ToString("dd/MMM/yyyy"),
                t.DenominationId,
                Type                 = t.IsSurplus ? "Surplus" : "Shortage",
                t.Amount,
                t.IsDifferentDenomination,
                t.IsProcessedOnRtgs,
                t.CommercialBankId,
                DepositBranchId      = t.DepositRequest != null ? (int?)t.DepositRequest.BouBranchId : null,
                DepositDenomAmount   = t.DepositRequest?.DepositRequestDenominations
                                           ?.FirstOrDefault(d => d.DenominationId == t.DenominationId)?.Amount,
                DepositDate          = t.DepositRequest != null ? t.DepositRequest.DepositDate.ToString("dd/MMM/yyyy") : null
            });

            return Ok(new { transactions = result });
        }

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.Create)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEodDiscrepancyReportDto dto)
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

                var report = new EodDiscrepancyReport
                {
                    ReferenceNumber   = dto.ReferenceNumber,
                    SessionDate       = dto.SessionDate,
                    BranchId          = dto.BranchId,
                    Remarks           = dto.Remarks,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                };

                await _cfbReportRepository.Create(report);

                // Link all unlinked transactions for this branch/session date
                var unlinked = await _cfbReportRepository.GetUnlinkedTransactions(dto.BranchId, dto.SessionDate);
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

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.Details)]
        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(EndOfDayDiscrepanciesController), FromAction = nameof(Index))]
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

            var branches      = await _configServiceClient.GetAllBouBranchesAsync();
            var branchName    = branches?.FirstOrDefault(b => b.Id == report.BranchId)?.Name ?? report.BranchId.ToString();
            var denominations = await _denominationRepository.FindAll();
            var banks         = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.DenomNames   = denominations?.Where(d => !d.IsDeleted)
                .ToDictionary(d => d.Id, d => d.Name) ?? new Dictionary<int, string>();
            ViewBag.BankNames    = banks?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
            ViewBag.BranchNames  = branches?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();

            var historyEntities = await _statusHistoryRepository.FindByCfbReportId(id.Value);
            var statusHistory = historyEntities.Select(h => new EodDiscrepancyReportStatusHistoryViewModel
            {
                OldStatusId   = h.OldStatusId,
                OldStatusName = h.OldStatusName,
                NewStatusId   = h.NewStatusId,
                NewStatusName = h.NewStatusName,
                ChangedBy     = h.ChangedBy,
                ChangedAt     = h.ChangedAt,
                Remarks       = h.Remarks
            });

            var vm = new EodDiscrepancyReportDetailsViewModel
            {
                Id                   = report.Id,
                ReferenceNumber      = report.ReferenceNumber,
                SessionDate          = report.SessionDate,
                BranchId             = report.BranchId,
                BranchName           = branchName,
                Remarks              = report.Remarks,
                CurrentStatusId      = report.CurrentStatusId,
                CurrentStatusName    = report.CurrentStatusName,
                CreatedBy            = report.CreatedBy,
                CreatedDate          = report.CreatedDate,
                Transactions         = report.Entries?.Select(e => e.BoardSessionSurplusShortage).ToList() ?? [],
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
            };

            return View(vm);
        }

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.Edit)]
        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(EndOfDayDiscrepanciesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var report = await _cfbReportRepository.FindById(id.Value);
            if (report == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View(report);
        }

        [RequirePermission(SystemPermissions.EndOfDayDiscrepancies.Edit)]
        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateEodDiscrepancyReportDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var report = await _cfbReportRepository.FindById(dto.Id);
                if (report == null) return NotFound();

                report.ReferenceNumber = dto.ReferenceNumber;
                report.SessionDate     = dto.SessionDate;
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
