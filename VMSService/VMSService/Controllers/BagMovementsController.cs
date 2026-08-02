using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.BagMovements;
using VMSService.Core.Models.Shared;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.BagMovements;
using VMSService.Core.Repositories.BagMovementStatusHistories;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Repositories.SubCategories;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BagMovementsController(
        IBagMovementRepository _bagMovementRepository,
        IBagMovementStatusHistoryRepository _bagMovementStatusHistoryRepository,
        IBagRepository _bagRepository,
        ISubCategoryRepository _subCategoryRepository,
        IVaultSectionRepository _vaultSectionRepository,
        IWorkflowServiceClient _workflowServiceClient,
        IWorkflowService _workflowService,
        IAttachmentRepository _attachmentRepo,
        IConfigurationsServiceClient _configServiceClient,
        IWebHostEnvironment _environment,
        ILogger<BagMovementsController> _logger) : Controller
    {
        private List<string> GetUserRoles()
            => User.FindAll(SystemPolicy.ClaimType.AppRole)
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

        private List<int> GetUserRoleIds()
            => User.FindAll("appRoleId")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => int.TryParse(c.Value.Split(":")[1], out var roleId) ? roleId : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();

        // Branches present among this user's role claims (branch names are passed through roles).
        private async Task<(List<int> BranchIds, Dictionary<int, string> BranchNames)> GetUserBranchContextAsync(List<string> roles)
        {
            var allBranches = await _configServiceClient.GetAllBouBranchesAsync();

            var branchIds = allBranches
                .Where(b => roles.Contains(b.Name))
                .Select(b => b.Id)
                .Distinct()
                .ToList();

            var branchNames = allBranches.ToDictionary(b => b.Id, b => b.Name);

            return (branchIds, branchNames);
        }

        // A movement's branch is derived from its first non-deleted bag; legacy single-bag rows fall back to Bag.
        private static Bag? GetMovementBranchBag(BagMovement movement)
            => movement.BagMovementBags.FirstOrDefault(mb => !mb.IsDeleted)?.Bag ?? movement.Bag;

        private Task<StatusTypeDto> GetPendingVerificationStatusAsync()
            => _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                EntityTypeName = StatusConstants.Entity.BagMovement,
                StatusName = StatusConstants.BagMovement.PendingVerification
            });

        [HttpGet]
        [Breadcrumb("Bag Movements", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.VerificationOfficer) && !roles.Contains(SystemPolicy.Roles.Treasury))
                return Forbid();

            try
            {
                var (userBranchIds, branchNames) = await GetUserBranchContextAsync(roles);
                if (userBranchIds.Count == 0)
                {
                    TempData["Info"] = "Your account is not associated with a branch.";
                    return View(new List<BagMovementViewModel>());
                }

                var movements = await _bagMovementRepository.FindAll();
                var workflowMovements = movements
                    .Where(m => m.CurrentStatusId != null)
                    .Select(m => (Movement: m, BranchBag: GetMovementBranchBag(m)))
                    .Where(x => x.BranchBag != null && userBranchIds.Contains(x.BranchBag.BranchId))
                    .Select(x => x.Movement)
                    .ToList();

                var allStatuses = await _workflowServiceClient.FindAllStatuses();
                var statusNameDict = allStatuses
                    .Where(s => s.EntityType?.Name == StatusConstants.Entity.BagMovement)
                    .ToDictionary(s => s.Id, s => s.Name);

                var allSections = await _vaultSectionRepository.FindAll();
                var sectionDict = allSections.ToDictionary(s => s.Id);

                var model = workflowMovements
                    .OrderByDescending(m => m.CreatedDate)
                    .Select(m =>
                    {
                        var branchBag = GetMovementBranchBag(m);
                        var bagRows = BuildBagRows(m);

                        return new BagMovementViewModel
                        {
                            Id = m.Id,
                            Bags = bagRows,
                            BranchId = branchBag?.BranchId ?? 0,
                            BranchName = branchBag != null ? branchNames.GetValueOrDefault(branchBag.BranchId) : null,
                            CurrentStatusId = m.CurrentStatusId,
                            CurrentStatusName = m.CurrentStatusId.HasValue
                                ? statusNameDict.GetValueOrDefault(m.CurrentStatusId.Value)
                                : null,
                            NewVaultSectionId = m.NewVaultSectionId,
                            NewVaultSectionName = m.NewVaultSection?.Name,
                            NewVaultName = sectionDict.GetValueOrDefault(m.NewVaultSectionId)?.Vault?.Name,
                            CreatedDate = m.CreatedDate,
                            CreatedBy = m.CreatedBy
                        };
                    })
                    .ToList();

                ViewBag.IsVerificationOfficer = roles.Contains(SystemPolicy.Roles.VerificationOfficer);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bag movements");
                TempData["Error"] = "An error occurred while retrieving bag movements. Please try again.";
                return View(new List<BagMovementViewModel>());
            }
        }

        [HttpGet]
        [Breadcrumb("New Movement Request", FromController = typeof(BagMovementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.VerificationOfficer))
                return Forbid();

            var (userBranchIds, branchNames) = await GetUserBranchContextAsync(roles);
            if (userBranchIds.Count == 0)
            {
                TempData["Error"] = "Your account is not associated with a branch.";
                return RedirectToAction(nameof(Index));
            }

            var model = await BuildFormViewModel(userBranchIds, branchNames);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBagMovementViewModel model)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.VerificationOfficer))
                return Forbid();

            try
            {
                var (userBranchIds, _) = await GetUserBranchContextAsync(roles);
                if (userBranchIds.Count == 0)
                {
                    TempData["Error"] = "Your account is not associated with a branch.";
                    return RedirectToAction(nameof(Create));
                }

                if (model.SelectedBagIds == null || model.SelectedBagIds.Count == 0)
                {
                    TempData["Error"] = "Select at least one bag to move.";
                    return RedirectToAction(nameof(Create));
                }

                var destinationSection = await _vaultSectionRepository.FindById(model.NewVaultSectionId);
                if (destinationSection == null || destinationSection.IsDeleted || !destinationSection.IsActive)
                {
                    TempData["Error"] = "Selected destination section was not found or is inactive.";
                    return RedirectToAction(nameof(Create));
                }

                if (destinationSection.Vault == null || !userBranchIds.Contains(destinationSection.Vault.BranchId))
                {
                    TempData["Error"] = "Destination section must be in your branch.";
                    return RedirectToAction(nameof(Create));
                }

                var pendingStatus = await GetPendingVerificationStatusAsync();
                if (pendingStatus == null)
                {
                    TempData["Error"] = "Bag movement workflow is not configured. Please contact an administrator.";
                    return RedirectToAction(nameof(Create));
                }

                var validation = await ValidateSelectedBags(model.SelectedBagIds, userBranchIds, destinationSection, pendingStatus.Id, ownBagIds: []);
                if (validation.Error != null)
                {
                    TempData["Error"] = validation.Error;
                    return RedirectToAction(nameof(Create));
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

                var movement = new BagMovement
                {
                    CurrentStatusId = pendingStatus.Id,
                    NewVaultSectionId = destinationSection.Id,
                    CreatedBy = username,
                    ModifiedBy = username
                };

                foreach (var bag in validation.Bags)
                {
                    movement.BagMovementBags.Add(new BagMovementBag
                    {
                        BagId = bag.Id,
                        OldVaultSectionId = bag.VaultSectionId!.Value,
                        CreatedBy = username,
                        ModifiedBy = username
                    });
                }

                await _bagMovementRepository.Create(movement);

                TempData["Success"] = "Bag movement request created successfully.";
                return RedirectToAction(nameof(Details), new { id = movement.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bag movement request");
                TempData["Error"] = "An error occurred while creating the bag movement request.";
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpGet]
        [Breadcrumb("Edit Movement Request", FromController = typeof(BagMovementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.VerificationOfficer))
                return Forbid();

            try
            {
                var movement = await _bagMovementRepository.FindById(id.Value);
                if (movement == null) return NotFound();

                var (userBranchIds, branchNames) = await GetUserBranchContextAsync(roles);
                var branchBag = GetMovementBranchBag(movement);
                if (branchBag == null || !userBranchIds.Contains(branchBag.BranchId))
                    return Forbid();

                var pendingStatus = await GetPendingVerificationStatusAsync();
                if (pendingStatus == null || movement.CurrentStatusId != pendingStatus.Id)
                {
                    TempData["Error"] = "This movement request can no longer be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var model = await BuildFormViewModel(userBranchIds, branchNames, movement);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bag movement {Id} for edit", id);
                TempData["Error"] = "An error occurred while loading the bag movement request.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateBagMovementViewModel model)
        {
            var roles = GetUserRoles();
            if (!roles.Contains(SystemPolicy.Roles.VerificationOfficer))
                return Forbid();

            try
            {
                var movement = await _bagMovementRepository.FindById(id);
                if (movement == null) return NotFound();

                var (userBranchIds, _) = await GetUserBranchContextAsync(roles);
                var branchBag = GetMovementBranchBag(movement);
                if (branchBag == null || !userBranchIds.Contains(branchBag.BranchId))
                    return Forbid();

                var pendingStatus = await GetPendingVerificationStatusAsync();
                if (pendingStatus == null || movement.CurrentStatusId != pendingStatus.Id)
                {
                    TempData["Error"] = "This movement request can no longer be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (model.SelectedBagIds == null || model.SelectedBagIds.Count == 0)
                {
                    TempData["Error"] = "Select at least one bag to move.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                var destinationSection = await _vaultSectionRepository.FindById(model.NewVaultSectionId);
                if (destinationSection == null || destinationSection.IsDeleted || !destinationSection.IsActive)
                {
                    TempData["Error"] = "Selected destination section was not found or is inactive.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (destinationSection.Vault == null || !userBranchIds.Contains(destinationSection.Vault.BranchId))
                {
                    TempData["Error"] = "Destination section must be in your branch.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                var existingBagIds = movement.BagMovementBags
                    .Where(mb => !mb.IsDeleted)
                    .Select(mb => mb.BagId)
                    .ToHashSet();

                var validation = await ValidateSelectedBags(model.SelectedBagIds, userBranchIds, destinationSection, pendingStatus.Id, existingBagIds);
                if (validation.Error != null)
                {
                    TempData["Error"] = validation.Error;
                    return RedirectToAction(nameof(Edit), new { id });
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var selectedBagIds = validation.Bags.Select(b => b.Id).ToHashSet();

                // Soft-delete rows for bags removed from the selection.
                foreach (var existingRow in movement.BagMovementBags.Where(mb => !mb.IsDeleted && !selectedBagIds.Contains(mb.BagId)))
                    existingRow.IsDeleted = true;

                // Add rows for newly-selected bags.
                foreach (var bag in validation.Bags.Where(b => !existingBagIds.Contains(b.Id)))
                {
                    movement.BagMovementBags.Add(new BagMovementBag
                    {
                        BagId = bag.Id,
                        OldVaultSectionId = bag.VaultSectionId!.Value,
                        CreatedBy = username,
                        ModifiedBy = username
                    });
                }

                movement.NewVaultSectionId = destinationSection.Id;
                movement.ModifiedBy = username;

                await _bagMovementRepository.Save();

                TempData["Success"] = "Bag movement request updated successfully.";
                return RedirectToAction(nameof(Details), new { id = movement.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating bag movement {Id}", id);
                TempData["Error"] = "An error occurred while updating the bag movement request.";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(BagMovementsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var movement = await _bagMovementRepository.FindById(id.Value);
                if (movement == null) return NotFound();

                var roles = GetUserRoles();
                var (userBranchIds, branchNames) = await GetUserBranchContextAsync(roles);
                var branchBag = GetMovementBranchBag(movement);
                if (branchBag == null || !userBranchIds.Contains(branchBag.BranchId))
                    return Forbid();

                var statusType = movement.CurrentStatusId.HasValue
                    ? await _workflowServiceClient.FindStatusById(movement.CurrentStatusId.Value)
                    : null;

                var newSection = await _vaultSectionRepository.FindById(movement.NewVaultSectionId);

                var availableTransitions = new List<WorkflowTransitionDto>();
                if (movement.CurrentStatusId.HasValue)
                {
                    var userRoleIds = GetUserRoleIds();
                    foreach (var roleId in userRoleIds)
                    {
                        var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                        {
                            ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                            EntityTypeName = StatusConstants.Entity.BagMovement,
                            CurrentStatusId = movement.CurrentStatusId.Value,
                            AppRole = roleId,
                        });
                        if (transitions != null) availableTransitions.AddRange(transitions);
                    }
                }

                var historyEntities = await _bagMovementStatusHistoryRepository.FindByBagMovementId(id.Value);
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

                var allAttachmentEntities = await _attachmentRepo.FindByEntityId(id.Value.ToString());
                var allAttachments = allAttachmentEntities
                    .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.BagMovement)
                    .Select(a => new AttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        UploadedDate = a.CreatedDate
                    }).ToList();

                var pendingStatus = await GetPendingVerificationStatusAsync();
                bool canEdit = roles.Contains(SystemPolicy.Roles.VerificationOfficer)
                    && pendingStatus != null
                    && movement.CurrentStatusId == pendingStatus.Id;

                var model = new BagMovementDetailsViewModel
                {
                    Id = movement.Id,
                    Bags = BuildBagRows(movement),
                    BranchId = branchBag.BranchId,
                    BranchName = branchNames.GetValueOrDefault(branchBag.BranchId),
                    NewVaultSectionId = movement.NewVaultSectionId,
                    NewVaultSectionName = movement.NewVaultSection?.Name,
                    NewVaultName = newSection?.Vault?.Name,
                    CurrentStatusId = movement.CurrentStatusId,
                    CurrentStatusName = statusType?.Name ?? string.Empty,
                    CreatedBy = movement.CreatedBy,
                    CreatedDate = movement.CreatedDate,
                    AvailableTransitions = availableTransitions.DistinctBy(t => t.ActionName).ToList(),
                    StatusHistory = statusHistory,
                    AllAttachments = allAttachments,
                    CanEdit = canEdit
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bag movement details for id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving bag movement details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var movement = await _bagMovementRepository.FindById(id);
                if (movement == null || !movement.CurrentStatusId.HasValue)
                {
                    TempData["Error"] = "Bag movement not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = GetUserRoles();
                var (userBranchIds, _) = await GetUserBranchContextAsync(roles);
                var branchBag = GetMovementBranchBag(movement);
                if (branchBag == null || !userBranchIds.Contains(branchBag.BranchId))
                    return Forbid();

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoleIds();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.BagMovement,
                    FromStatusId = movement.CurrentStatusId.Value,
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
                        StatusConstants.Entity.BagMovement,
                        username);
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.BagMovement,
                    id,
                    movement.CurrentStatusId.Value,
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
                _logger.LogError(ex, "Error executing action {Action} on bag movement {Id}", actionName, id);
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

        // Builds the eligible-bags selection table plus destination list, for both Create (existingMovement == null) and Edit.
        private async Task<CreateBagMovementViewModel> BuildFormViewModel(
            List<int> userBranchIds, Dictionary<int, string> branchNames, BagMovement? existingMovement = null)
        {
            var pendingStatus = await GetPendingVerificationStatusAsync();
            var pendingBagIds = pendingStatus != null
                ? (await _bagMovementRepository.GetBagIdsWithPendingMovement(pendingStatus.Id)).ToHashSet()
                : new HashSet<int>();

            // Bags already on this movement remain selectable even though they're technically "pending" (for this same movement).
            var ownBagIds = existingMovement?.BagMovementBags
                .Where(mb => !mb.IsDeleted)
                .Select(mb => mb.BagId)
                .ToHashSet() ?? new HashSet<int>();

            var bags = await _bagRepository.FindAll();
            var eligibleBags = bags
                .Where(b => !b.IsDeleted && b.VaultSectionId.HasValue && userBranchIds.Contains(b.BranchId)
                    && (!pendingBagIds.Contains(b.Id) || ownBagIds.Contains(b.Id)))
                .ToList();

            var sections = await _vaultSectionRepository.FindAll();
            var activeSections = sections
                .Where(s => !s.IsDeleted && s.IsActive && s.Vault != null && userBranchIds.Contains(s.Vault.BranchId))
                .ToList();
            var sectionDict = sections.ToDictionary(s => s.Id);

            var subCategories = await _subCategoryRepository.FindAll();
            var subCategoryDict = subCategories.ToDictionary(sc => sc.Id, sc => sc.Name);

            var branchNameDisplay = string.Join(", ", userBranchIds
                .Select(branchId => branchNames.GetValueOrDefault(branchId))
                .Where(name => !string.IsNullOrEmpty(name)));

            return new CreateBagMovementViewModel
            {
                Id = existingMovement?.Id,
                BranchName = branchNameDisplay,
                SelectedBagIds = ownBagIds.ToList(),
                NewVaultSectionId = existingMovement?.NewVaultSectionId ?? 0,
                Bags = eligibleBags.Select(b => new BagSelectionRowViewModel
                {
                    BagId = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    VaultName = sectionDict.GetValueOrDefault(b.VaultSectionId!.Value)?.Vault?.Name,
                    VaultSectionName = b.VaultSection?.Name,
                    TotalAmount = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Sum(bd => bd.DepositRequestDenomination.Amount),
                    SubCategoryName = b.SubCategoryId.HasValue
                        ? subCategoryDict.GetValueOrDefault(b.SubCategoryId.Value)
                        : null,
                    IsSelected = ownBagIds.Contains(b.Id)
                }).ToList(),
                VaultSectionList = activeSections.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Vault?.Name} — {s.Name}"
                }).ToList()
            };
        }

        // Validates a set of selected bag ids for Create/Edit; ownBagIds are bags already on the movement being edited
        // (exempt from the "already pending" check, since they're pending as part of this very movement).
        private async Task<(List<Bag> Bags, string? Error)> ValidateSelectedBags(
            List<int> selectedBagIds, List<int> userBranchIds, VaultSection destinationSection, int pendingStatusId, ICollection<int> ownBagIds)
        {
            var bags = new List<Bag>();

            foreach (var bagId in selectedBagIds.Distinct())
            {
                var bag = await _bagRepository.FindById(bagId);
                if (bag == null || bag.IsDeleted)
                    return (bags, $"A selected bag (id {bagId}) was not found.");

                if (!userBranchIds.Contains(bag.BranchId))
                    return (bags, $"Bag {bag.ReferenceNumber} does not belong to your branch.");

                if (bag.BranchId != destinationSection.Vault!.BranchId)
                    return (bags, $"Bag {bag.ReferenceNumber} is not in the same branch as the destination section.");

                if (!bag.VaultSectionId.HasValue)
                    return (bags, $"Bag {bag.ReferenceNumber} is not currently assigned to a vault section.");

                if (bag.VaultSectionId.Value == destinationSection.Id)
                    return (bags, $"Bag {bag.ReferenceNumber} is already in the destination section.");

                if (!ownBagIds.Contains(bag.Id) && await _bagMovementRepository.HasPendingForBag(bag.Id, pendingStatusId))
                    return (bags, $"Bag {bag.ReferenceNumber} already has a pending movement request.");

                bags.Add(bag);
            }

            return (bags, null);
        }

        // Builds the per-bag "From" rows for Index/Details, from BagMovementBags, falling back to the legacy single-bag pair.
        private static List<BagSelectionRowViewModel> BuildBagRows(BagMovement movement)
        {
            var childRows = movement.BagMovementBags
                .Where(mb => !mb.IsDeleted)
                .Select(mb => new BagSelectionRowViewModel
                {
                    BagId = mb.BagId,
                    ReferenceNumber = mb.Bag?.ReferenceNumber ?? $"Bag {mb.BagId}",
                    VaultName = mb.OldVaultSection?.Vault?.Name,
                    VaultSectionName = mb.OldVaultSection?.Name
                })
                .ToList();

            if (childRows.Count > 0)
                return childRows;

            if (movement.BagId.HasValue)
            {
                return
                [
                    new BagSelectionRowViewModel
                    {
                        BagId = movement.BagId.Value,
                        ReferenceNumber = movement.Bag?.ReferenceNumber ?? $"Bag {movement.BagId.Value}",
                        VaultName = movement.OldVaultSection?.Vault?.Name,
                        VaultSectionName = movement.OldVaultSection?.Name
                    }
                ];
            }

            return [];
        }

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploadedAttachmentIds = new List<long>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "bagmovements", entityId.ToString());
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
                        FilePath = Path.Combine("uploads", "bagmovements", entityId.ToString(), uniqueName),
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
    }
}
