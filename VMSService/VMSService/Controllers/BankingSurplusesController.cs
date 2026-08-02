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
        VMSServiceDbContext _db,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.BankingSurpluses.View)]
        [Breadcrumb("Banking Surpluses", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var surpluses = await _surplusRepo.FindAll();
            var branches  = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.BranchNames = branches?.ToDictionary(b => b.Id, b => b.Name) ?? new Dictionary<int, string>();
            return View(surpluses);
        }

        [RequirePermission(SystemPermissions.BankingSurpluses.Create)]
        [HttpGet]
        [Breadcrumb("Generate", FromController = typeof(BankingSurplusesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Generate()
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
                .Where(b => b.Name.Equals("Banking", StringComparison.OrdinalIgnoreCase))
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
        public async Task<IActionResult> Generate([FromBody] CreateBankingSurplusDto dto)
        {
            try
            {
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

                var surplus = new BankingSurplus
                {
                    ReferenceNumber   = dto.ReferenceNumber,
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
                return Json(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = surplus.Id }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
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

            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":").Last(), out var n) ? n : 0)
                .Where(n => n > 0)
                .ToList();

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var role in userRoles)
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitionsAsync(
                    SystemPolicy.SystemSettings.AppAcronym,
                    StatusConstants.Entity.BankingSurpluses,
                    surplus.CurrentStatusId,
                    role);
                if (transitions != null)
                    availableTransitions.AddRange(transitions.Select(t => new WorkflowTransitionDto
                    {
                        ActionName             = t.ActionName,
                        DisplayLabel           = t.DisplayLabel,
                        RequiresAttachments    = t.RequiresAttachments,
                        AttachmentRequirements = t.AttachmentRequirements,
                    }));
            }
            availableTransitions = availableTransitions.DistinctBy(t => t.ActionName).ToList();

            var historyEntities = await _historyRepo.FindByBankingSurplusId(id.Value);
            var allAttachmentIds = historyEntities
                .Where(h => !string.IsNullOrWhiteSpace(h.AttachmentIds))
                .SelectMany(h => h.AttachmentIds!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0).Where(n => n > 0))
                .ToList();

            var allAttachments = (await _attachmentRepo.FindByEntityId(id.Value.ToString()))
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

        private List<int> GetUserRoles()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
    }
}
