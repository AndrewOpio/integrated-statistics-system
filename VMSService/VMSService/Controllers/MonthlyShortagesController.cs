using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.MonthlyShortages;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.MonthlyShortages;
using VMSService.Core.Repositories.MonthlyShortageStatusHistories;
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
    public class MonthlyShortagesController(
        IMonthlyShortageRepository _shortageRepo,
        IMonthlyShortageDetailRepository _detailRepo,
        IMonthlyShortageStatusHistoryRepository _historyRepo,
        IAttachmentRepository _attachmentRepo,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationRepository _denominationRepo,
        VMSServiceDbContext _db,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.MonthlyShortages.View)]
        [Breadcrumb("Monthly Shortages", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var shortages = await _shortageRepo.FindAll();
            return View(shortages);
        }

        [RequirePermission(SystemPermissions.MonthlyShortages.Generate)]
        [HttpGet]
        [Breadcrumb("Generate", FromController = typeof(MonthlyShortagesController), FromAction = nameof(Index))]
        public IActionResult Generate()
        {
            return View();
        }

        [RequirePermission(SystemPermissions.MonthlyShortages.Generate)]
        [HttpPost]
        public async Task<IActionResult> Generate(int month, int year)
        {
            try
            {
                var periodStart = new DateTime(year, month, 1);
                var periodEnd   = periodStart.AddMonths(1);

                var existing = await _shortageRepo.FindByPeriod(periodStart);
                if (existing != null && existing.CurrentStatusName != StatusConstants.MonthlyShortage.Draft)
                {
                    TempData["Error"] = $"A shortage report for {periodStart:MMMM yyyy} already exists and is in {existing.CurrentStatusName} status. It cannot be regenerated.";
                    return RedirectToAction(nameof(Generate));
                }

                var eodReports = await _db.EodDiscrepancyReports
                    .AsNoTracking()
                    .Include(r => r.Entries)
                        .ThenInclude(e => e.BoardSessionSurplusShortage)
                    .Where(r => r.SessionDate >= periodStart && r.SessionDate < periodEnd
                             && r.CurrentStatusName == StatusConstants.CfbReport.Approved)
                    .ToListAsync();

                if (!eodReports.Any())
                {
                    TempData["Error"] = $"No approved EOD reports found for {periodStart:MMMM yyyy}.";
                    return RedirectToAction(nameof(Generate));
                }

                // Exclude "Banking" commercial bank transactions
                var allBanks = await _configServiceClient.GetAllCommercialBankAsync();
                var bankingIds = allBanks?
                    .Where(b => b.Name.Equals(StatusConstants.CommercialBank.Banking, StringComparison.OrdinalIgnoreCase))
                    .Select(b => b.Id)
                    .ToHashSet() ?? new HashSet<int>();

                // Aggregate per branch — include all branches regardless of net position
                var branchTotals = eodReports
                    .SelectMany(r => r.Entries.Select(e => e.BoardSessionSurplusShortage))
                    .Where(t => !bankingIds.Contains(t.CommercialBankId))
                    .GroupBy(t => t.BranchId)
                    .Select(g => new
                    {
                        BranchId      = g.Key,
                        TotalSurplus  = g.Where(t => t.IsSurplus).Sum(t => t.Amount),
                        TotalShortage = g.Where(t => !t.IsSurplus).Sum(t => t.Amount),
                        NetShortage   = g.Where(t => !t.IsSurplus).Sum(t => t.Amount)
                                      - g.Where(t => t.IsSurplus).Sum(t => t.Amount),
                    })
                    .ToList();

                if (!branchTotals.Any())
                {
                    TempData["Error"] = $"No approved EOD activity found for any branch in {periodStart:MMMM yyyy}.";
                    return RedirectToAction(nameof(Generate));
                }

                decimal grandSurplus  = branchTotals.Sum(b => b.TotalSurplus);
                decimal grandShortage = branchTotals.Sum(b => b.TotalShortage);
                decimal grandNet      = branchTotals.Sum(b => b.NetShortage);

                int initialStatusId = 0;
                string initialStatusName = StatusConstants.MonthlyShortage.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.MonthlyShortages,
                        StatusName         = StatusConstants.MonthlyShortage.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { }

                if (existing != null)
                {
                    // Update header totals
                    existing.TotalSurplus  = grandSurplus;
                    existing.TotalShortage = grandShortage;
                    existing.NetShortage   = grandNet;
                    await _shortageRepo.Update(existing);

                    // Replace branch lines
                    await _detailRepo.DeleteByMonthlyShortageId(existing.Id);
                    foreach (var b in branchTotals)
                        await _detailRepo.Create(new MonthlyShortageDetail
                        {
                            MonthlyShortageId = existing.Id,
                            BranchId          = b.BranchId,
                            TotalSurplus      = b.TotalSurplus,
                            TotalShortage     = b.TotalShortage,
                            NetShortage       = b.NetShortage,
                        });

                    TempData["Success"] = $"Shortage report for {periodStart:MMMM yyyy} regenerated. {branchTotals.Count} branch(es), net shortage: {grandNet:N0}.";
                }
                else
                {
                    int seq = await _shortageRepo.CountForPeriod(year, month) + 1;
                    var shortage = new MonthlyShortage
                    {
                        ReferenceNumber   = ReferenceNumberGenerator.MonthlyShortage(year, month, seq),
                        Period            = periodStart,
                        TotalSurplus      = grandSurplus,
                        TotalShortage     = grandShortage,
                        NetShortage       = grandNet,
                        CurrentStatusId   = initialStatusId,
                        CurrentStatusName = initialStatusName,
                    };
                    await _shortageRepo.Create(shortage);

                    foreach (var b in branchTotals)
                        await _detailRepo.Create(new MonthlyShortageDetail
                        {
                            MonthlyShortageId = shortage.Id,
                            BranchId          = b.BranchId,
                            TotalSurplus      = b.TotalSurplus,
                            TotalShortage     = b.TotalShortage,
                            NetShortage       = b.NetShortage,
                        });

                    TempData["Success"] = $"Shortage report for {periodStart:MMMM yyyy} created. {branchTotals.Count} branch(es), net shortage: {grandNet:N0}.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
                return RedirectToAction(nameof(Generate));
            }
        }

        [RequirePermission(SystemPermissions.MonthlyShortages.Details)]
        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(MonthlyShortagesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var shortage = await _shortageRepo.FindById(id.Value);
            if (shortage == null) return NotFound();

            try
            {
                var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlyShortages,
                    StatusName         = shortage.CurrentStatusName,
                });
                if (status?.Id > 0 && status.Id != shortage.CurrentStatusId)
                {
                    shortage.CurrentStatusId = status.Id;
                    await _shortageRepo.Update(shortage);
                }
            }
            catch { }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlyShortages,
                    CurrentStatusId    = shortage.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var branches    = await _configServiceClient.GetAllBouBranchesAsync();
            var branchNames = branches?.ToDictionary(b => b.Id, b => b.Name) ?? new();

            var detailEntities = await _detailRepo.FindByMonthlyShortageId(id.Value);
            var branchDetails  = detailEntities.Select(d => new MonthlyShortageDetailViewModel
            {
                BranchId      = d.BranchId,
                BranchName    = branchNames.TryGetValue(d.BranchId, out var bn) ? bn : d.BranchId.ToString(),
                TotalSurplus  = d.TotalSurplus,
                TotalShortage = d.TotalShortage,
                NetShortage   = d.NetShortage,
            }).ToList();

            var historyEntities = await _historyRepo.FindByMonthlyShortageId(id.Value);
            var statusHistory   = new List<MonthlyShortageStatusHistoryViewModel>();
            foreach (var h in historyEntities)
            {
                var entry = new MonthlyShortageStatusHistoryViewModel
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
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.MonthlyShortages)
                .Select(a => new AttachmentViewModel
                {
                    Id       = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                });

            var vm = new MonthlyShortageDetailsViewModel
            {
                Id                   = shortage.Id,
                ReferenceNumber      = shortage.ReferenceNumber,
                Period               = shortage.Period,
                TotalSurplus         = shortage.TotalSurplus,
                TotalShortage        = shortage.TotalShortage,
                NetShortage          = shortage.NetShortage,
                Remarks              = shortage.Remarks,
                CurrentStatusId      = shortage.CurrentStatusId,
                CurrentStatusName    = shortage.CurrentStatusName,
                CreatedBy            = shortage.CreatedBy,
                CreatedDate          = shortage.CreatedDate,
                BranchDetails        = branchDetails,
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                Attachments          = allAttachments,
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> BranchBreakdown(int shortageId, int branchId)
        {
            var shortage = await _shortageRepo.FindById(shortageId);
            if (shortage == null) return NotFound();

            var periodStart = new DateTime(shortage.Period.Year, shortage.Period.Month, 1);
            var periodEnd   = periodStart.AddMonths(1);

            var entries = await _db.EodDiscrepancyReportEntries
                .AsNoTracking()
                .Include(e => e.EodDiscrepancyReport)
                .Include(e => e.BoardSessionSurplusShortage)
                .Where(e => e.BoardSessionSurplusShortage.BranchId == branchId
                         && e.EodDiscrepancyReport.SessionDate >= periodStart
                         && e.EodDiscrepancyReport.SessionDate < periodEnd
                         && e.EodDiscrepancyReport.CurrentStatusName == StatusConstants.CfbReport.Approved)
                .OrderBy(e => e.EodDiscrepancyReport.SessionDate)
                .ToListAsync();

            var banks = await _configServiceClient.GetAllCommercialBankAsync();
            var bankMap = banks?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
            var bankingIds = banks?
                .Where(b => b.Name.Equals(StatusConstants.CommercialBank.Banking, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Id)
                .ToHashSet() ?? new HashSet<int>();

            var denominations = await _denominationRepo.FindAll();
            var denomMap = denominations?.ToDictionary(d => d.Id, d => d.Name) ?? new Dictionary<int, string>();

            var transactions = entries
                .Where(e => !bankingIds.Contains(e.BoardSessionSurplusShortage.CommercialBankId))
                .Select(e => new
                {
                    sessionDate    = e.EodDiscrepancyReport.SessionDate.ToString("dd MMM yyyy"),
                    isSurplus      = e.BoardSessionSurplusShortage.IsSurplus,
                    amount         = e.BoardSessionSurplusShortage.Amount,
                    commercialBank = bankMap.TryGetValue(e.BoardSessionSurplusShortage.CommercialBankId, out var name) ? name : "—",
                    denomination   = denomMap.TryGetValue(e.BoardSessionSurplusShortage.DenominationId, out var dname) ? dname : "—",
                });

            return Ok(new { transactions });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var shortage = await _shortageRepo.FindById(id);
            if (shortage == null)
            {
                TempData["Error"] = "Shortage record not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.MonthlyShortages,
                    FromStatusId   = shortage.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.MonthlyShortages,
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
                    entityType:      StatusConstants.Entity.MonthlyShortages,
                    entityId:        id,
                    currentStatusId: shortage.CurrentStatusId,
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
            var uploadDir   = Path.Combine(_environment.WebRootPath, "uploads", "shortages", entityId.ToString());
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
                    FilePath                          = Path.Combine("uploads", "shortages", entityId.ToString(), uniqueName),
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
