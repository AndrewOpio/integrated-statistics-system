using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.MonthlyShortageReimbursements;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.MonthlyShortageReimbursements;
using VMSService.Core.Repositories.MonthlyShortages;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Authorization;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Controllers
{
    [Authorize]
    public class MonthlyShortageReimbursementsController(
        IMonthlyShortageReimbursementRepository _reimbursementRepo,
        IMonthlyShortageReimbursementStatusHistoryRepository _historyRepo,
        IMonthlyShortageRepository _shortageRepo,
        IMonthlyShortageDetailRepository _shortageDetailRepo,
        IDenominationRepository _denominationRepo,
        IAttachmentRepository _attachmentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IWebHostEnvironment _environment) : Controller
    {
        [RequirePermission(SystemPermissions.MonthlyShortageReimbursements.View)]
        [Breadcrumb("Shortage Reimbursements", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var reimbursements = await _reimbursementRepo.FindAll();
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var shortages = await _shortageRepo.FindAll();

            var branchMap = branches?.ToDictionary(b => b.Id, b => b.Name) ?? [];
            var shortageMap = shortages.ToDictionary(s => s.Id, s => s.ReferenceNumber);

            var userBranchIds = User.FindAll("branchId")
                .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var filtered = userBranchIds.Count > 0
                ? reimbursements.Where(r => userBranchIds.Contains(r.BranchId)).ToList()
                : [];

            var rows = filtered.Select(r => new MonthlyShortageReimbursementIndexViewModel
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                ShortageReference = shortageMap.TryGetValue(r.MonthlyShortageId, out var sRef) ? sRef : r.MonthlyShortageId.ToString(),
                BranchName = branchMap.TryGetValue(r.BranchId, out var bn) ? bn : r.BranchId.ToString(),
                TotalAmount = r.TotalAmount,
                CreatedDate = r.CreatedDate,
                CurrentStatusName = r.CurrentStatusName,
            }).ToList();

            var vm = new ReimbursementIndexPageViewModel { Reimbursements = rows };

            return View(vm);
        }

        [RequirePermission(SystemPermissions.MonthlyShortageReimbursements.View)]
        [Breadcrumb("Details", FromController = typeof(MonthlyShortageReimbursementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var reimbursement = await _reimbursementRepo.FindById(id);
            if (reimbursement == null) return NotFound();

            var shortage = await _shortageRepo.FindById(reimbursement.MonthlyShortageId);
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var branchMap = branches?.ToDictionary(b => b.Id, b => b.Name) ?? [];

            // Sync status ID with WorkflowService
            var status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                EntityTypeName     = StatusConstants.Entity.MonthlyShortageReimbursements,
                StatusName         = reimbursement.CurrentStatusName,
            });
            if (status?.Id > 0 && status.Id != reimbursement.CurrentStatusId)
            {
                reimbursement.CurrentStatusId = status.Id;
                await _reimbursementRepo.Update(reimbursement);
            }

            // Collect available transitions across all user roles
            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.MonthlyShortageReimbursements,
                    CurrentStatusId    = reimbursement.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null) availableTransitions.AddRange(transitions);
            }

            // Load status history with attachments
            var historyEntities = await _historyRepo.FindByReimbursementId(id);
            var statusHistory = new List<ReimbursementStatusHistoryViewModel>();
            foreach (var h in historyEntities)
            {
                var attachments = new List<AttachmentViewModel>();
                if (!string.IsNullOrWhiteSpace(h.AttachmentIds))
                {
                    foreach (var rawId in h.AttachmentIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(rawId.Trim(), out var attId))
                        {
                            var att = await _attachmentRepo.FindById(attId);
                            if (att != null && !att.IsDeleted)
                                attachments.Add(new AttachmentViewModel
                                {
                                    Id = (long)att.Id,
                                    FileName = att.FileName,
                                    FilePath = att.FilePath,
                                    FileType = att.FileType,
                                    FileSize = att.FileSize,
                                    UploadedDate = att.CreatedDate,
                                });
                        }
                    }
                }
                statusHistory.Add(new ReimbursementStatusHistoryViewModel
                {
                    OldStatusName = h.OldStatusName,
                    NewStatusName = h.NewStatusName,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Remarks = h.Remarks,
                    Attachments = attachments,
                });
            }

            var vm = new ReimbursementDetailsViewModel
            {
                Id = reimbursement.Id,
                ReferenceNumber = reimbursement.ReferenceNumber,
                ShortageReference = shortage?.ReferenceNumber ?? reimbursement.MonthlyShortageId.ToString(),
                ShortagePeriod = shortage?.Period ?? DateTime.MinValue,
                BranchName = branchMap.TryGetValue(reimbursement.BranchId, out var bn) ? bn : reimbursement.BranchId.ToString(),
                TotalAmount = reimbursement.TotalAmount,
                Remarks = reimbursement.Remarks,
                CreatedBy = reimbursement.CreatedBy,
                CreatedDate = reimbursement.CreatedDate,
                CurrentStatusId = reimbursement.CurrentStatusId,
                CurrentStatusName = reimbursement.CurrentStatusName,
                Denominations = reimbursement.Denominations.Select(d => new ReimbursementDenominationDetailViewModel
                {
                    DenominationName = d.DenominationName,
                    DenominationValue = d.DenominationValue,
                    Amount = d.Amount,
                    NumberOfPieces = d.NumberOfPieces,
                }).OrderBy(d => d.DenominationValue).ToList(),
                AvailableTransitions = availableTransitions,
                StatusHistory = statusHistory,
            };

            return View(vm);
        }

        [RequirePermission(SystemPermissions.MonthlyShortageReimbursements.Create)]
        [HttpGet]
        [Breadcrumb("Capture Reimbursement", FromController = typeof(MonthlyShortageReimbursementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var shortages = await _shortageRepo.FindAll();
            var denominations = await _denominationRepo.FindAll();

            var vm = new CreateReimbursementViewModel
            {
                ShortageOptions = shortages.Select(s => new ShortageOptionViewModel
                {
                    Id = s.Id,
                    ReferenceNumber = s.ReferenceNumber,
                    Period = s.Period,
                }).ToList(),
                DenominationOptions = denominations
                    .Where(d => !d.IsDeleted)
                    .Select(d => new DenomOptionViewModel
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Value = d.Value,
                    }).OrderBy(d => d.Value).ToList(),
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> BranchesForShortage(int shortageId)
        {
            var details = await _shortageDetailRepo.FindByMonthlyShortageId(shortageId);
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var branchMap = branches?.ToDictionary(b => b.Id, b => b.Name) ?? [];

            var userBranchIds = User.FindAll("branchId")
                .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var result = details
                .Where(d => userBranchIds.Count == 0 || userBranchIds.Contains(d.BranchId))
                .Select(d => new
                {
                    branchId = d.BranchId,
                    branchName = branchMap.TryGetValue(d.BranchId, out var name) ? name : d.BranchId.ToString(),
                });

            return Ok(result);
        }

        [RequirePermission(SystemPermissions.MonthlyShortageReimbursements.Create)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReimbursementRequest request)
        {
            if (request.MonthlyShortageId == 0 || request.BranchId == 0)
                return BadRequest(new { success = false, message = "Monthly Shortage and Branch are required." });

            if (request.Denominations == null || request.Denominations.Count == 0)
                return BadRequest(new { success = false, message = "At least one denomination row is required." });

            var shortage = await _shortageRepo.FindById(request.MonthlyShortageId);
            if (shortage == null)
                return NotFound(new { success = false, message = "Monthly Shortage not found." });

            if (await _reimbursementRepo.ExistsForShortageAndBranch(request.MonthlyShortageId, request.BranchId))
                return Conflict(new { success = false, message = "A reimbursement for this branch and month has already been captured." });

            var denominations = await _denominationRepo.FindAll();
            var denomMap = denominations.ToDictionary(d => d.Id);

            var username = User.Identity?.Name ?? "system";
            var totalAmount = request.Denominations.Sum(d => d.Amount);

            var reimbursement = new MonthlyShortageReimbursement
            {
                ReferenceNumber = ReferenceNumberGenerator.ShortageReimbursement(shortage.Period, request.BranchId),
                MonthlyShortageId = request.MonthlyShortageId,
                BranchId = request.BranchId,
                TotalAmount = totalAmount,
                Remarks = request.Remarks,
                CurrentStatusName = StatusConstants.MonthlyShortageReimbursement.Draft,
                CreatedBy = username,
                ModifiedBy = username,
                Denominations = request.Denominations.Select(d =>
                {
                    denomMap.TryGetValue(d.DenominationId, out var denom);
                    return new MonthlyShortageReimbursementDenomination
                    {
                        DenominationId = d.DenominationId,
                        DenominationName = denom?.Name ?? d.DenominationId.ToString(),
                        DenominationValue = denom?.Value ?? 0,
                        Amount = d.Amount,
                        NumberOfPieces = d.NumberOfPieces,
                        CreatedBy = username,
                        ModifiedBy = username,
                    };
                }).ToList(),
            };

            await _reimbursementRepo.Create(reimbursement);

            return Ok(new { success = true, id = reimbursement.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var reimbursement = await _reimbursementRepo.FindById(id);
            if (reimbursement == null)
            {
                TempData["Error"] = "Reimbursement record not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.MonthlyShortageReimbursements,
                    FromStatusId   = reimbursement.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.MonthlyShortageReimbursements,
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
                    entityType:      StatusConstants.Entity.MonthlyShortageReimbursements,
                    entityId:        id,
                    currentStatusId: reimbursement.CurrentStatusId,
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
            var uploadDir   = Path.Combine(_environment.WebRootPath, "uploads", "reimbursements", entityId.ToString());
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
                    FilePath                          = Path.Combine("uploads", "reimbursements", entityId.ToString(), uniqueName),
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
