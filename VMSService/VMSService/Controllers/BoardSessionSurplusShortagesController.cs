using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.BoardSessionSurplusShortages;
using VMSService.Core.Models.Shared;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.BoardSessionSurplusShortages;
using VMSService.Core.Repositories.BoardSessionSurplusShortageStatusHistories;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BoardSessionSurplusShortagesController(
        IBoardSessionSurplusShortageRepository _repo,
        IBoardSessionSurplusShortageStatusHistoryRepository _historyRepo,
        IAttachmentRepository _attachmentRepo,
        ICommercialBankRepository _commercialBankRepository,
        IStockService _stockService,
        IConfigurationsServiceClient _configServiceClient,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IWebHostEnvironment _environment,
        ILogger<BoardSessionSurplusShortagesController> _logger) : Controller
    {
        [HttpGet]
        [Breadcrumb("CFB Requests", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();

                // Branches present among this user's role claims (branch names are passed through roles).
                var branchIds = allBranches
                    .Where(b => userRoles.Contains(b.Name))
                    .Select(b => b.Id)
                    .Distinct()
                    .ToList();

                if (branchIds.Count == 0)
                    return View(new List<BoardSessionSurplusShortageIndexItemViewModel>());

                var records = new List<BoardSessionSurplusShortage>();
                foreach (var branchId in branchIds)
                    records.AddRange(await _repo.FindByBranchId(branchId));

                if (records.Count == 0)
                    return View(new List<BoardSessionSurplusShortageIndexItemViewModel>());

                var branchDict = allBranches.ToDictionary(b => b.Id, b => b.Name);

                var allBanks = await _commercialBankRepository.FindAll();
                var bankDict = allBanks.ToDictionary(b => b.Id, b => b.Name);

                var allDenominations = await _stockService.GetAllDenominations();
                var denomDict = allDenominations.ToDictionary(d => d.Id);

                var statusIds = records.Select(r => r.CurrentStatusId).Distinct().ToList();
                var statusNameDict = new Dictionary<int, string?>();
                foreach (var statusId in statusIds)
                {
                    var status = await _workflowServiceClient.FindStatusById(statusId);
                    statusNameDict[statusId] = status?.Name;
                }

                var model = records
                    .OrderByDescending(r => r.CreatedDate)
                    .Select(r => new BoardSessionSurplusShortageIndexItemViewModel
                    {
                        Id = r.Id,
                        StockProcessingSessionId = r.StockProcessingSessionId,
                        SessionReferenceNumber = r.StockProcessingSession?.ReferenceNumber,
                        Amount = r.Amount,
                        IsSurplus = r.IsSurplus,
                        IsDifferentDenomination = r.IsDifferentDenomination,
                        DenominationName = denomDict.GetValueOrDefault(r.DenominationId)?.Name,
                        DenominationValue = denomDict.GetValueOrDefault(r.DenominationId)?.Value ?? 0m,
                        CommercialBankName = bankDict.GetValueOrDefault(r.CommercialBankId),
                        BranchName = branchDict.GetValueOrDefault(r.BranchId),
                        CurrentStatusId = r.CurrentStatusId,
                        CurrentStatusName = statusNameDict.GetValueOrDefault(r.CurrentStatusId) ?? string.Empty,
                        CreatedDate = r.CreatedDate
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CFB requests");
                TempData["Error"] = "An error occurred while retrieving CFB requests. Please try again.";
                return View(new List<BoardSessionSurplusShortageIndexItemViewModel>());
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(BoardSessionSurplusShortagesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var record = await _repo.FindById(id.Value);
                if (record == null) return NotFound();

                var allBranches = await _configServiceClient.GetAllBouBranchesAsync();
                var branchName = allBranches.FirstOrDefault(b => b.Id == record.BranchId)?.Name;

                var allBanks = await _commercialBankRepository.FindAll();
                var bankName = allBanks.FirstOrDefault(b => b.Id == record.CommercialBankId)?.Name;

                var allDenominations = await _stockService.GetAllDenominations();
                var denomination = allDenominations.FirstOrDefault(d => d.Id == record.DenominationId);

                var statusType = await _workflowServiceClient.FindStatusById(record.CurrentStatusId);

                // Available workflow transitions for this user
                var userRoleIds = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var rid) ? rid : (int?)null)
                    .Where(r => r.HasValue).Select(r => r!.Value).ToList();

                var availableTransitions = new List<WorkflowTransitionDto>();
                foreach (var roleId in userRoleIds)
                {
                    var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.BoardSessionSurplusShortage,
                        CurrentStatusId = record.CurrentStatusId,
                        AppRole = roleId,
                    });
                    if (transitions != null) availableTransitions.AddRange(transitions);
                }

                // Status history ("Comments")
                var historyEntities = await _historyRepo.FindByBoardSessionSurplusShortageId(id.Value);
                var statusHistory = new List<StatusHistoryViewModel>();
                foreach (var h in historyEntities.OrderBy(h => h.ChangedAt))
                {
                    var oldStatus = await _workflowServiceClient.FindStatusById(h.OldStatusId);
                    var newStatus = await _workflowServiceClient.FindStatusById(h.NewStatusId);

                    var attachmentFiles = new List<AttachmentViewModel>();
                    if (!string.IsNullOrEmpty(h.AttachmentIds))
                    {
                        var attachmentIds = h.AttachmentIds.Split(',')
                            .Where(a => int.TryParse(a.Trim(), out _))
                            .Select(a => int.Parse(a.Trim())).ToList();

                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = await _attachmentRepo.FindById(attachmentId);
                            if (attachment != null && !attachment.IsDeleted)
                            {
                                attachmentFiles.Add(new AttachmentViewModel
                                {
                                    Id = attachment.Id,
                                    FileName = attachment.FileName,
                                    FilePath = attachment.FilePath,
                                    FileType = attachment.FileType,
                                    FileSize = attachment.FileSize,
                                    UploadedDate = attachment.CreatedDate
                                });
                            }
                        }
                    }

                    statusHistory.Add(new StatusHistoryViewModel
                    {
                        OldStatusName = oldStatus?.Name ?? h.OldStatusId.ToString(),
                        NewStatusName = newStatus?.Name ?? h.NewStatusId.ToString(),
                        ChangedBy = h.ChangedBy,
                        ChangedAt = h.ChangedAt,
                        Remarks = h.Remarks,
                        Attachments = attachmentFiles
                    });
                }

                // All entity attachments
                var allAttachmentEntities = await _attachmentRepo.FindByEntityId(id.Value.ToString());
                var allAttachments = allAttachmentEntities
                    .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.BoardSessionSurplusShortage)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        UploadedDate = a.CreatedDate
                    }).ToList();

                var model = new BoardSessionSurplusShortageDetailsViewModel
                {
                    Id = record.Id,
                    StockProcessingSessionId = record.StockProcessingSessionId,
                    SessionReferenceNumber = record.StockProcessingSession?.ReferenceNumber,
                    Amount = record.Amount,
                    IsSurplus = record.IsSurplus,
                    IsDifferentDenomination = record.IsDifferentDenomination,
                    IsProcessedOnRtgs = record.IsProcessedOnRtgs,
                    RtgsCode = record.RtgsCode,
                    CommercialBankId = record.CommercialBankId,
                    CommercialBankName = bankName,
                    BranchId = record.BranchId,
                    BranchName = branchName,
                    DenominationId = record.DenominationId,
                    DenominationName = denomination?.Name,
                    DenominationValue = denomination?.Value ?? 0m,
                    CfbBoxId = record.CfbBoxId,
                    CfbBoxName = record.CfbBox?.Name,
                    CurrentStatusId = record.CurrentStatusId,
                    CurrentStatusName = statusType?.Name ?? string.Empty,
                    CreatedBy = record.CreatedBy,
                    CreatedDate = record.CreatedDate,
                    AvailableTransitions = availableTransitions.DistinctBy(t => t.ActionName).ToList(),
                    StatusHistory = statusHistory,
                    AllAttachments = allAttachments
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving CFB request details for id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving CFB request details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var record = await _repo.FindById(id);
                if (record == null)
                {
                    TempData["Error"] = "CFB request not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.BoardSessionSurplusShortage,
                    FromStatusId = record.CurrentStatusId,
                    ActionName = actionName,
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
                        TempData["Error"] = $"Missing required attachments: {string.Join(", ", missingRequired)}";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.BoardSessionSurplusShortage,
                        username);
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.BoardSessionSurplusShortage,
                    id,
                    record.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks,
                    uploadedAttachmentIds);

                TempData[isExecuted ? "Success" : "Error"] = isExecuted
                    ? "Action completed successfully."
                    : $"Failed to execute '{actionName}'. You may not have the required role or the action is not valid for the current status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on CFB request {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            try
            {
                var attachment = await _attachmentRepo.FindById(id);
                if (attachment == null || attachment.IsDeleted)
                    return NotFound();

                var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound();

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                    await stream.CopyToAsync(memory);
                memory.Position = 0;

                return File(memory, attachment.FileType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId}", id);
                return NotFound();
            }
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "boardsessionsurplusshortage", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var requirement in requirements)
            {
                var file = Request.Form.Files[$"attachments_{requirement.Id}"];
                if (file == null || file.Length == 0) continue;

                try
                {
                    var fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                    var allowedFormats = requirement.AllowedFormats.Split(',')
                        .Select(f => f.Trim().ToLower()).ToList();

                    var formatMap = new Dictionary<string, List<string>>
                    {
                        { "excel", ["xls", "xlsx"] },
                        { "word",  ["doc", "docx"] },
                        { "docx",  ["doc", "docx"] },
                        { "jpg",   ["jpg", "jpeg"] },
                        { "jpeg",  ["jpg", "jpeg"] }
                    };

                    bool isValid = allowedFormats.Any(fmt =>
                        formatMap.TryGetValue(fmt, out var exts) ? exts.Contains(fileExtension) : fmt == fileExtension);

                    if (!isValid)
                    {
                        _logger.LogWarning("Invalid file format {Ext} for requirement {Id}", fileExtension, requirement.Id);
                        continue;
                    }

                    var uniqueName = $"{requirement.Id}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileNameWithoutExtension(file.FileName)}.{fileExtension}";
                    var filePath = Path.Combine(uploadsPath, uniqueName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    var attachment = new Attachment
                    {
                        FileName = file.FileName,
                        FilePath = Path.Combine("uploads", "boardsessionsurplusshortage", entityId.ToString(), uniqueName),
                        FileType = file.ContentType,
                        FileSize = file.Length,
                        WorkflowTransitionId = workflowTransitionId,
                        TransitionAttachmentRequirementId = requirement.Id,
                        EntityId = entityId.ToString(),
                        EntityType = entityType,
                        CreatedBy = username,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = username,
                        ModifiedDate = DateTime.Now,
                        IsDeleted = false
                    };

                    await _attachmentRepo.Create(attachment);
                    uploadedAttachmentIds.Add(attachment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading attachment for requirement {Id}", requirement.Id);
                }
            }

            return uploadedAttachmentIds;
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
