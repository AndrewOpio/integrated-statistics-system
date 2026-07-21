using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.MonthlyCommercialBankSettlements;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class MonthlyCommercialBankSettlementsController(
        IMonthlyCommercialBankSettlementRepository _settlementRepo,
        IMonthlyCommercialBankSettlementStatusHistoryRepository _historyRepo,
        IAttachmentRepository _attachmentRepo,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationRepository _denominationRepo,
        VMSServiceDbContext _db,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.MonthlyCommercialBankSettlements.View)]
        [Breadcrumb("Commercial Bank Settlements", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var settlements = await _settlementRepo.FindAll();
            var banks       = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.BankNames = banks?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
            return View(settlements);
        }

        [RequirePermission(SystemPermissions.MonthlyCommercialBankSettlements.Generate)]
        [HttpGet]
        [Breadcrumb("Generate", FromController = typeof(MonthlyCommercialBankSettlementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Generate()
        {
            var banks = await _configServiceClient.GetAllCommercialBankAsync();
            ViewBag.Banks = banks?.ToList() ?? new List<CommercialBankDto>();
            return View();
        }

        [RequirePermission(SystemPermissions.MonthlyCommercialBankSettlements.Generate)]
        [HttpPost]
        public async Task<IActionResult> Generate(int bankId, int month, int year)
        {
            try
            {
                var periodStart = new DateTime(year, month, 1);
                var periodEnd   = periodStart.AddMonths(1);

                var existing = await _settlementRepo.FindByPeriodAndBank(periodStart, bankId);
                if (existing != null && existing.CurrentStatusName != StatusConstants.MonthlyCommercialBankSettlement.Draft)
                {
                    TempData["Error"] = $"A settlement for this bank in {periodStart:MMMM yyyy} already exists and is in {existing.CurrentStatusName} status. It cannot be regenerated.";
                    return RedirectToAction(nameof(Generate));
                }

                // Aggregate surplus/shortage transactions for this commercial bank in the period
                // from approved EOD reports only
                var entries = await _db.EodDiscrepancyReportEntries
                    .AsNoTracking()
                    .Include(e => e.EodDiscrepancyReport)
                    .Include(e => e.BoardSessionSurplusShortage)
                    .Where(e => e.BoardSessionSurplusShortage.CommercialBankId == bankId
                             && e.EodDiscrepancyReport.SessionDate >= periodStart
                             && e.EodDiscrepancyReport.SessionDate < periodEnd
                             && e.EodDiscrepancyReport.CurrentStatusName == StatusConstants.CfbReport.Approved)
                    .ToListAsync();

                if (!entries.Any())
                {
                    TempData["Error"] = $"No approved EOD report transactions found for this bank in {periodStart:MMMM yyyy}.";
                    return RedirectToAction(nameof(Generate));
                }

                var transactions  = entries.Select(e => e.BoardSessionSurplusShortage).ToList();
                var totalSurplus  = transactions.Where(t => t.IsSurplus).Sum(t => t.Amount);
                var totalShortage = transactions.Where(t => !t.IsSurplus).Sum(t => t.Amount);
                var netPosition   = totalSurplus - totalShortage;

                int initialStatusId      = 0;
                string initialStatusName = StatusConstants.MonthlyCommercialBankSettlement.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.MonthlyCommercialBankSettlements,
                        StatusName         = StatusConstants.MonthlyCommercialBankSettlement.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { }

                var banks    = await _configServiceClient.GetAllCommercialBankAsync();
                var bankName = banks?.FirstOrDefault(b => b.Id == bankId)?.Name ?? bankId.ToString();

                if (existing != null)
                {
                    existing.TotalSurplus  = totalSurplus;
                    existing.TotalShortage = totalShortage;
                    existing.NetPosition   = netPosition;
                    await _settlementRepo.Update(existing);

                    TempData["Success"] = $"Settlement for {bankName} — {periodStart:MMMM yyyy} regenerated. Surplus: {totalSurplus:N0}, Shortage: {totalShortage:N0}, Net: {netPosition:N0}.";
                    return RedirectToAction(nameof(Details), new { id = existing.Id });
                }
                else
                {
                    var settlement = new MonthlyCommercialBankSettlement
                    {
                        ReferenceNumber   = $"CBS/{year}/{month:D2}",
                        CommercialBankId  = bankId,
                        Period            = periodStart,
                        TotalSurplus      = totalSurplus,
                        TotalShortage     = totalShortage,
                        NetPosition       = netPosition,
                        CurrentStatusId   = initialStatusId,
                        CurrentStatusName = initialStatusName,
                    };
                    await _settlementRepo.Create(settlement);

                    TempData["Success"] = $"Settlement for {bankName} — {periodStart:MMMM yyyy} created. Surplus: {totalSurplus:N0}, Shortage: {totalShortage:N0}, Net: {netPosition:N0}.";
                    return RedirectToAction(nameof(Details), new { id = settlement.Id });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
                return RedirectToAction(nameof(Generate));
            }
        }

        [RequirePermission(SystemPermissions.MonthlyCommercialBankSettlements.Details)]
        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(MonthlyCommercialBankSettlementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var settlement = await _settlementRepo.FindById(id.Value);
            if (settlement == null) return NotFound();

            try
            {
                var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlyCommercialBankSettlements,
                    StatusName         = settlement.CurrentStatusName,
                });
                if (status?.Id > 0 && status.Id != settlement.CurrentStatusId)
                {
                    settlement.CurrentStatusId = status.Id;
                    await _settlementRepo.Update(settlement);
                }
            }
            catch { }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlyCommercialBankSettlements,
                    CurrentStatusId    = settlement.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var banks    = await _configServiceClient.GetAllCommercialBankAsync();
            var bankName = banks?.FirstOrDefault(b => b.Id == settlement.CommercialBankId)?.Name ?? settlement.CommercialBankId.ToString();

            var historyEntities = await _historyRepo.FindBySettlementId(id.Value);
            var statusHistory   = new List<MonthlyCommercialBankSettlementStatusHistoryViewModel>();
            foreach (var h in historyEntities)
            {
                var entry = new MonthlyCommercialBankSettlementStatusHistoryViewModel
                {
                    OldStatusId   = h.OldStatusId,
                    OldStatusName = h.OldStatusName,
                    NewStatusId   = h.NewStatusId,
                    NewStatusName = h.NewStatusName,
                    ChangedBy     = h.ChangedBy,
                    ChangedAt     = h.ChangedAt,
                    Remarks       = h.Remarks,
                };

                if (!string.IsNullOrEmpty(h.AttachmentIds))
                {
                    var attachments = new List<AttachmentViewModel>();
                    foreach (var aid in h.AttachmentIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(aid.Trim(), out var attachmentId))
                        {
                            var att = await _attachmentRepo.FindById(attachmentId);
                            if (att != null && !att.IsDeleted)
                                attachments.Add(new AttachmentViewModel
                                {
                                    Id       = att.Id,
                                    FileName = att.FileName,
                                    FilePath = att.FilePath,
                                    FileSize = att.FileSize,
                                    FileType = att.FileType,
                                });
                        }
                    }
                    entry.Attachments = attachments;
                }

                statusHistory.Add(entry);
            }

            var allAttachments = (await _attachmentRepo.FindByEntityId(id.Value.ToString()))
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.MonthlyCommercialBankSettlements)
                .Select(a => new AttachmentViewModel
                {
                    Id       = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                });

            var vm = new MonthlyCommercialBankSettlementDetailsViewModel
            {
                Id                   = settlement.Id,
                ReferenceNumber      = settlement.ReferenceNumber,
                CommercialBankId     = settlement.CommercialBankId,
                CommercialBankName   = bankName,
                Period               = settlement.Period,
                TotalSurplus         = settlement.TotalSurplus,
                TotalShortage        = settlement.TotalShortage,
                NetPosition          = settlement.NetPosition,
                Remarks              = settlement.Remarks,
                CurrentStatusId      = settlement.CurrentStatusId,
                CurrentStatusName    = settlement.CurrentStatusName,
                CreatedBy            = settlement.CreatedBy ?? string.Empty,
                CreatedDate          = settlement.CreatedDate,
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                Attachments          = allAttachments,
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Breakdown(int id)
        {
            var settlement = await _settlementRepo.FindById(id);
            if (settlement == null) return Json(new { error = "Not found" });

            var periodStart = new DateTime(settlement.Period.Year, settlement.Period.Month, 1);
            var periodEnd   = periodStart.AddMonths(1);

            var entries = await _db.EodDiscrepancyReportEntries
                .AsNoTracking()
                .Include(e => e.EodDiscrepancyReport)
                .Include(e => e.BoardSessionSurplusShortage)
                .Where(e => e.BoardSessionSurplusShortage.CommercialBankId == settlement.CommercialBankId
                         && e.EodDiscrepancyReport.SessionDate >= periodStart
                         && e.EodDiscrepancyReport.SessionDate < periodEnd
                         && e.EodDiscrepancyReport.CurrentStatusName == StatusConstants.CfbReport.Approved)
                .OrderBy(e => e.EodDiscrepancyReport.SessionDate)
                .ToListAsync();

            var denominations = await _denominationRepo.FindAll();
            var denomMap = denominations?.ToDictionary(d => d.Id, d => d.Name) ?? [];

            var transactions = entries.Select(e => new
            {
                sessionDate   = e.EodDiscrepancyReport.SessionDate.ToString("dd MMM yyyy"),
                isSurplus     = e.BoardSessionSurplusShortage?.IsSurplus ?? false,
                amount        = e.BoardSessionSurplusShortage?.Amount ?? 0,
                denomination  = e.BoardSessionSurplusShortage != null
                    && denomMap.TryGetValue(e.BoardSessionSurplusShortage.DenominationId, out var dn) ? dn : "—",
            }).ToList();

            return Json(new { transactions });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var settlement = await _settlementRepo.FindById(id);
            if (settlement == null)
            {
                TempData["Error"] = "Settlement record not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.MonthlyCommercialBankSettlements,
                    FromStatusId   = settlement.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.MonthlyCommercialBankSettlements,
                        username);

                    var missingRequired = transition.AttachmentRequirements
                        .Where(r => r.IsRequired && !(Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0))
                        .Select(r => r.AttachmentName)
                        .ToList();

                    if (missingRequired.Any())
                    {
                        TempData["Error"] = $"Required attachment(s) missing: {string.Join(", ", missingRequired)}.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.MonthlyCommercialBankSettlements,
                    entityId:        id,
                    currentStatusId: settlement.CurrentStatusId,
                    actionName:      actionName,
                    username:        username,
                    userRoles:       userRoles,
                    remarks:         remarks,
                    attachmentIds:   uploadedAttachmentIds);

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

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _attachmentRepo.FindById(id);
            if (attachment == null || attachment.IsDeleted)
                return NotFound();

            var fullPath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var memory = new MemoryStream();
            await using (var stream = new FileStream(fullPath, FileMode.Open))
                await stream.CopyToAsync(memory);
            memory.Position = 0;

            return File(memory, attachment.FileType, attachment.FileName);
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedIds = new List<long>();
            var uploadDir   = Path.Combine(_environment.WebRootPath, "uploads", "cb-settlements", entityId.ToString());
            Directory.CreateDirectory(uploadDir);

            foreach (var req in requirements)
            {
                var file = Request.Form.Files[$"attachments_{req.Id}"];
                if (file == null || file.Length == 0) continue;

                var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var fullPath   = Path.Combine(uploadDir, uniqueName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var attachment = new Attachment
                {
                    FileName                          = file.FileName,
                    FilePath                          = Path.Combine("uploads", "cb-settlements", entityId.ToString(), uniqueName),
                    FileType                          = file.ContentType,
                    FileSize                          = file.Length,
                    WorkflowTransitionId              = workflowTransitionId,
                    TransitionAttachmentRequirementId = req.Id,
                    EntityId                          = entityId.ToString(),
                    EntityType                        = entityType,
                    IsDeleted                         = false,
                    CreatedBy                         = username,
                };

                await _attachmentRepo.Create(attachment);
                uploadedIds.Add(attachment.Id);
            }

            return uploadedIds;
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
