using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.BankingSurpluses;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.BankingSurpluses;
using VMSService.Core.Repositories.BankingSurplusStatusHistories;
using VMSService.Core.Repositories.Denominations;
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
    public class BankingSurplusesController(
        IBankingSurplusRepository _surplusRepo,
        IBankingSurplusStatusHistoryRepository _historyRepo,
        IAttachmentRepository _attachmentRepo,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IConfigurationsServiceClient _configServiceClient,
        IDenominationRepository _denominationRepo,
        VMSServiceDbContext _db,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.BankingSurpluses.View)]
        [Breadcrumb("Banking Surpluses", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var surpluses = await _surplusRepo.FindAll();
            var branchIds = GetUserBranchIds();
            surpluses = surpluses.Where(s => branchIds.Contains(s.BranchId)).ToList();
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.BranchNames = branches?.ToDictionary(b => b.Id, b => b.Name) ?? [];
            return View(surpluses);
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Create)]
        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(BankingSurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View();
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Create)]
        [HttpGet]
        public async Task<IActionResult> ComputeNetPosition(int branchId, string period)
        {
            if (!DateTime.TryParse(period + "-01", out var periodStart))
                return BadRequest(new { error = "Invalid period." });

            var periodEnd = periodStart.AddMonths(1);

            var allBanks   = await _configServiceClient.GetAllCommercialBankAsync();
            var bankingIds = allBanks?
                .Where(b => b.Name.Equals(StatusConstants.CommercialBank.Banking, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Id)
                .ToHashSet() ?? new HashSet<int>();

            var entries = await _db.EodDiscrepancyReportEntries
                .AsNoTracking()
                .Include(e => e.EodDiscrepancyReport)
                .Include(e => e.BoardSessionSurplusShortage)
                .Where(e => e.BoardSessionSurplusShortage.BranchId == branchId
                         && bankingIds.Contains(e.BoardSessionSurplusShortage.CommercialBankId)
                         && e.EodDiscrepancyReport.SessionDate >= periodStart
                         && e.EodDiscrepancyReport.SessionDate < periodEnd
                         && e.EodDiscrepancyReport.CurrentStatusName == StatusConstants.CfbReport.Approved)
                .ToListAsync();

            decimal totalSurplus  = entries.Where(e => e.BoardSessionSurplusShortage.IsSurplus).Sum(e => e.BoardSessionSurplusShortage.Amount);
            decimal totalShortage = entries.Where(e => !e.BoardSessionSurplusShortage.IsSurplus).Sum(e => e.BoardSessionSurplusShortage.Amount);
            decimal netSurplus    = totalSurplus - totalShortage;

            return Ok(new
            {
                totalSurplus,
                totalShortage,
                netSurplus,
                isSurplus  = netSurplus > 0,
                hasEntries = entries.Any()
            });
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Create)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBankingSurplusDto dto)
        {
            try
            {
                var periodStart = new DateTime(dto.Period.Year, dto.Period.Month, 1);
                var existing = await _surplusRepo.FindByBranchAndPeriod(dto.BranchId, periodStart);
                if (existing != null)
                    return BadRequest(new { success = false, message = $"A banking surplus for this branch and period already exists (Ref: {existing.ReferenceNumber})." });

                int initialStatusId      = 0;
                string initialStatusName = StatusConstants.BankingSurplus.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.BankingSurpluses,
                        StatusName         = StatusConstants.BankingSurplus.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { }

                var username = User.Identity?.Name ?? "system";

                var branch = await _configServiceClient.GetBouBranchByIdAsync(dto.BranchId);
                var branchCode = !string.IsNullOrWhiteSpace(branch?.BranchCode)
                    ? branch.BranchCode
                    : (branch?.Name?.Replace(" ", "").ToUpper()[..Math.Min(3, branch.Name.Replace(" ", "").Length)] ?? "BRN");
                int seq = await _surplusRepo.CountForPeriodAndBranch(dto.BranchId, dto.Period.Year, dto.Period.Month) + 1;

                var surplus = new BankingSurplus
                {
                    ReferenceNumber   = ReferenceNumberGenerator.BankingSurplus(dto.Period.Year, dto.Period.Month, branchCode, seq),
                    BranchId          = dto.BranchId,
                    Period            = dto.Period,
                    Amount            = dto.Amount,
                    DepositDate       = dto.DepositDate,
                    Remarks           = dto.Remarks,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                    CreatedBy         = username,
                    ModifiedBy        = username,
                    CreatedDate       = DateTime.Now,
                    ModifiedDate      = DateTime.Now,
                };

                await _surplusRepo.Create(surplus);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = surplus.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message });
            }
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Edit)]
        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(BankingSurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var surplus = await _surplusRepo.FindById(id.Value);
            if (surplus == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Branches = branches?.ToList() ?? new();
            return View(surplus);
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Edit)]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [FromBody] CreateBankingSurplusDto dto)
        {
            try
            {
                var surplus = await _surplusRepo.FindById(id);
                if (surplus == null) return Json(new { success = false, message = "Record not found." });

                var username = User.Identity?.Name ?? "system";

                surplus.BranchId        = dto.BranchId;
                surplus.Period          = dto.Period;
                surplus.Amount          = dto.Amount;
                surplus.DepositDate     = dto.DepositDate;
                surplus.Remarks         = dto.Remarks;
                surplus.ModifiedBy      = username;
                surplus.ModifiedDate    = DateTime.Now;

                await _surplusRepo.Update(surplus);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = surplus.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { success = false, message });
            }
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Details)]
        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(BankingSurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var surplus = await _surplusRepo.FindById(id.Value);
            if (surplus == null) return NotFound();

            var branches  = await _configServiceClient.GetAllBouBranchesAsync();
            var branchMap = branches?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();

            try
            {
                var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.BankingSurpluses,
                    StatusName         = surplus.CurrentStatusName,
                });
                if (status?.Id > 0 && status.Id != surplus.CurrentStatusId)
                {
                    surplus.CurrentStatusId = status.Id;
                    await _surplusRepo.Update(surplus);
                }
            }
            catch { }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.BankingSurpluses,
                    CurrentStatusId    = surplus.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }
            availableTransitions = availableTransitions.DistinctBy(t => t.ActionName).ToList();

            var allBanks   = await _configServiceClient.GetAllCommercialBankAsync();
            var bankMap    = allBanks?.ToDictionary(b => b.Id, b => b.Name) ?? [];
            var bankingIds = allBanks?
                .Where(b => b.Name.Equals(StatusConstants.CommercialBank.Banking, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Id)
                .ToHashSet() ?? new HashSet<int>();

            var periodStart = new DateTime(surplus.Period.Year, surplus.Period.Month, 1);
            var periodEnd   = periodStart.AddMonths(1);

            var eodEntries = await _db.EodDiscrepancyReportEntries
                .AsNoTracking()
                .Include(e => e.EodDiscrepancyReport)
                .Include(e => e.BoardSessionSurplusShortage)
                .Where(e => e.BoardSessionSurplusShortage.BranchId == surplus.BranchId
                         && bankingIds.Contains(e.BoardSessionSurplusShortage.CommercialBankId)
                         && e.EodDiscrepancyReport.SessionDate >= periodStart
                         && e.EodDiscrepancyReport.SessionDate < periodEnd
                         && e.EodDiscrepancyReport.CurrentStatusName == StatusConstants.CfbReport.Approved)
                .OrderBy(e => e.EodDiscrepancyReport.SessionDate)
                .ToListAsync();

            var denominations = await _denominationRepo.FindAll();
            var denomMap = denominations?.ToDictionary(d => d.Id, d => d.Name) ?? [];

            var eodEntryViewModels = eodEntries.Select(e => new BankingSurplusEodEntryViewModel
            {
                SessionDate        = e.EodDiscrepancyReport.SessionDate,
                IsSurplus          = e.BoardSessionSurplusShortage.IsSurplus,
                Amount             = e.BoardSessionSurplusShortage.Amount,
                CommercialBankName = bankMap.TryGetValue(e.BoardSessionSurplusShortage.CommercialBankId, out var bn) ? bn : "—",
                DenominationName   = denomMap.TryGetValue(e.BoardSessionSurplusShortage.DenominationId, out var dn) ? dn : "—",
            }).ToList();

            var historyEntities = await _historyRepo.FindByBankingSurplusId(id.Value);
            var allAttachmentIds = historyEntities
                .Where(h => !string.IsNullOrWhiteSpace(h.AttachmentIds))
                .SelectMany(h => h.AttachmentIds!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0).Where(n => n > 0))
                .ToList();

            var allAttachments = (await _attachmentRepo.FindByEntityId(id.Value.ToString()))
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.BankingSurpluses)
                .Select(a => new AttachmentViewModel
                {
                    Id       = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                });

            var statusHistory = historyEntities.Select(h => new BankingSurplusStatusHistoryViewModel
            {
                OldStatusId   = h.OldStatusId,
                OldStatusName = h.OldStatusName,
                NewStatusId   = h.NewStatusId,
                NewStatusName = h.NewStatusName,
                ChangedBy     = h.ChangedBy,
                ChangedAt     = h.ChangedAt,
                Remarks       = h.Remarks,
                Attachments   = allAttachmentIds
                    .Where(aid => (h.AttachmentIds ?? "").Contains(aid.ToString()))
                    .Select(aid => allAttachments.FirstOrDefault(a => a.Id == aid))
                    .Where(a => a != null)
                    .Select(a => a!)
                    .ToList(),
            }).ToList();

            var vm = new BankingSurplusDetailsViewModel
            {
                Id                   = surplus.Id,
                ReferenceNumber      = surplus.ReferenceNumber,
                BranchId             = surplus.BranchId,
                BranchName           = branchMap.TryGetValue(surplus.BranchId, out var bn) ? bn : $"Branch {surplus.BranchId}",
                Period               = surplus.Period,
                Amount               = surplus.Amount,
                DepositDate          = surplus.DepositDate,
                Remarks              = surplus.Remarks,
                CurrentStatusId      = surplus.CurrentStatusId,
                CurrentStatusName    = surplus.CurrentStatusName,
                CreatedBy            = surplus.CreatedBy,
                CreatedDate          = surplus.CreatedDate,
                EodEntries           = eodEntryViewModels,
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                Attachments          = allAttachments,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var surplus = await _surplusRepo.FindById(id);
            if (surplus == null)
            {
                TempData["Error"] = "Banking surplus record not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.BankingSurpluses,
                    FromStatusId   = surplus.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    var missingRequired = transition.AttachmentRequirements
                        .Where(r => r.IsRequired && !(Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0))
                        .Select(r => r.AttachmentName)
                        .ToList();

                    if (missingRequired.Any())
                    {
                        TempData["Error"] = $"Required attachment(s) missing: {string.Join(", ", missingRequired)}.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.BankingSurpluses,
                        username);
                }

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.BankingSurpluses,
                    entityId:        id,
                    currentStatusId: surplus.CurrentStatusId,
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
            var uploadDir   = Path.Combine(FileStorageConstants.UploadsRoot, "banking-surpluses", entityId.ToString());
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
                    FilePath                          = Path.Combine("uploads", "banking-surpluses", entityId.ToString(), uniqueName),
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


        private List<int> GetUserBranchIds()
        {
            return User.FindAll("branchId")
                .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
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
