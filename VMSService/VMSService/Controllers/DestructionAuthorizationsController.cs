using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.DestructionAuthorizations;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.DestructionAuthorizationStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DestructionAuthorizationsController : Controller
    {
        private readonly IDestructionAuthorizationRepository _authorizationRepository;
        private readonly IDestructionAuthorizationStatusHistoryRepository _statusHistoryRepository;
        private readonly IStockService _stockService;
        private readonly IStockPositionService _stockPositionService;
        private readonly IConfigurationsServiceClient _configServiceClient;
        private readonly IWorkflowServiceClient _workflowServiceClient;
        private readonly IWorkflowService _workflowService;
        private readonly IAttachmentRepository _attachmentRepository;

        public DestructionAuthorizationsController(
            IDestructionAuthorizationRepository authorizationRepository,
            IDestructionAuthorizationStatusHistoryRepository statusHistoryRepository,
            IStockService stockService,
            IStockPositionService stockPositionService,
            IConfigurationsServiceClient configServiceClient,
            IWorkflowServiceClient workflowServiceClient,
            IWorkflowService workflowService,
            IAttachmentRepository attachmentRepository)
        {
            _authorizationRepository  = authorizationRepository;
            _statusHistoryRepository  = statusHistoryRepository;
            _stockService             = stockService;
            _stockPositionService     = stockPositionService;
            _configServiceClient      = configServiceClient;
            _workflowServiceClient    = workflowServiceClient;
            _workflowService          = workflowService;
            _attachmentRepository     = attachmentRepository;
        }

        // Available bags per denomination = current Cancelled-category stock value at the branch,
        // converted to pieces, divided by the fixed bag capacity (StockConstants.DestructionTypes.NotesPerDestructionBag).
        private async Task<(Dictionary<int, int> AvailableBags, string? Error)> GetAvailableCancelledBagsAsync(int branchId, List<int> denominationIds)
        {
            var categories = await _stockService.GetAllCategories();
            var cancelledCategory = categories?.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Cancelled);
            if (cancelledCategory == null)
                return (new Dictionary<int, int>(), "Cancelled category is not configured in Stock Service.");

            var denominations = await _stockService.GetAllDenominations();
            var matrix = await _stockPositionService.GetStockMatrixAsync(branchId, [cancelledCategory.Id], denominationIds);

            var result = new Dictionary<int, int>();
            foreach (var denominationId in denominationIds)
            {
                var denomination = denominations?.FirstOrDefault(d => d.Id == denominationId);
                var value = matrix.TryGetValue(denominationId, out var byCategory)
                    ? byCategory.GetValueOrDefault(cancelledCategory.Id, 0m)
                    : 0m;

                var pieces = denomination is { Value: > 0 } ? value / denomination.Value : 0m;
                result[denominationId] = (int)(pieces / StockConstants.DestructionTypes.NotesPerDestructionBag);
            }

            return (result, null);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCancelledBags(int branchId)
        {
            var denominations = await _stockService.GetAllDenominations();
            var denominationIds = denominations?.Select(d => d.Id).ToList() ?? new List<int>();
            var (availableBags, error) = await GetAvailableCancelledBagsAsync(branchId, denominationIds);
            if (error != null)
                return Ok(new { bags = new Dictionary<int, int>(), error });

            return Ok(new { bags = availableBags });
        }

        [Breadcrumb("Destruction Authorizations", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var authorizations = await _authorizationRepository.FindAll();
            return View(authorizations);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Denominations = await _stockService.GetAllDenominations();
            ViewBag.Branches      = branches?.ToList() ?? new();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDestructionAuthorizationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var existingActive = await _authorizationRepository.FindActiveByBranch(dto.BranchId);
                if (existingActive != null)
                    return BadRequest(new { message = $"This branch already has an active authorization ({existingActive.ReferenceNumber}, status {existingActive.CurrentStatusName}). It must reach a final status (Approved, Rejected, or Amendment Approved) before a new one can be created." });

                var denominationIds = dto.Denominations.Select(d => d.DenominationId).ToList();
                var (availableBags, bagsError) = await GetAvailableCancelledBagsAsync(dto.BranchId, denominationIds);
                if (bagsError != null)
                    return BadRequest(new { message = bagsError });

                var allDenominations = await _stockService.GetAllDenominations();
                foreach (var denom in dto.Denominations)
                {
                    var requestedBags = int.TryParse(denom.NumberOfBags, out var bags) ? bags : 0;
                    var available     = availableBags.GetValueOrDefault(denom.DenominationId, 0);
                    if (requestedBags > available)
                    {
                        var denomName = allDenominations?.FirstOrDefault(d => d.Id == denom.DenominationId)?.Name ?? $"ID {denom.DenominationId}";
                        return BadRequest(new { message = $"Number of bags for {denomName} exceeds the {available:N0} bag(s) currently available in the Cancelled category for this branch." });
                    }
                }

                int initialStatusId = 0;
                string initialStatusName = StatusConstants.DestructionAuthorization.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                        StatusName         = StatusConstants.DestructionAuthorization.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                var authorization = new DestructionAuthorization
                {
                    ReferenceNumber   = dto.ReferenceNumber,
                    BranchId          = dto.BranchId,
                    Date              = dto.Date,
                    CurrentStatusId   = initialStatusId,
                    CurrentStatusName = initialStatusName,
                    DestructionAuthorizationDenominations = dto.Denominations.Select(d => new DestructionAuthorizationDenomination
                    {
                        DenominationId = d.DenominationId,
                        NumberOfBags   = d.NumberOfBags,
                        Amount         = d.Amount
                    }).ToList()
                };

                await _authorizationRepository.Create(authorization);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var authorization = await _authorizationRepository.FindById(id.Value);
            if (authorization == null) return NotFound();

            var denominations = await _stockService.GetAllDenominations();

            // If created before workflow was configured, CurrentStatusId is 0 — resolve and persist the real ID
            if (authorization.CurrentStatusId == 0)
            {
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                        StatusName         = authorization.CurrentStatusName,
                    });
                    if (draftStatus?.Id > 0)
                    {
                        authorization.CurrentStatusId = draftStatus.Id;
                        await _authorizationRepository.Update(authorization);
                    }
                }
                catch { /* workflow may not be reachable */ }
            }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.DestructionAuthorization,
                    CurrentStatusId    = authorization.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            var historyEntities = await _statusHistoryRepository.FindByDestructionAuthorizationId(id.Value);
            var statusHistory = historyEntities.Select(h => new DestructionAuthorizationStatusHistoryViewModel
            {
                OldStatusId   = h.OldStatusId,
                OldStatusName = h.OldStatusName,
                NewStatusId   = h.NewStatusId,
                NewStatusName = h.NewStatusName,
                ChangedBy     = h.ChangedBy,
                ChangedAt     = h.ChangedAt,
                Remarks       = h.Remarks
            });

            var branches   = await _configServiceClient.GetAllBouBranchesAsync();
            var branchName = branches?.FirstOrDefault(b => b.Id == authorization.BranchId)?.Name ?? authorization.BranchId.ToString();

            var allAttachments = (await _attachmentRepository.FindByEntityId(id.Value.ToString()))
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.DestructionAuthorization)
                .Select(a => new AttachmentViewModel
                {
                    Id           = a.Id,
                    FileName     = a.FileName,
                    FilePath     = a.FilePath,
                    FileType     = a.FileType,
                    FileSize     = a.FileSize,
                    UploadedDate = a.CreatedDate,
                })
                .ToList();

            var vm = new DestructionAuthorizationDetailsViewModel
            {
                Id                   = authorization.Id,
                ReferenceNumber      = authorization.ReferenceNumber,
                BranchId             = authorization.BranchId,
                BranchName           = branchName,
                Date                 = authorization.Date,
                CurrentStatusId      = authorization.CurrentStatusId,
                CurrentStatusName    = authorization.CurrentStatusName,
                CreatedBy            = authorization.CreatedBy,
                CreatedDate          = authorization.CreatedDate,
                Denominations        = authorization.DestructionAuthorizationDenominations ?? [],
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                AllAttachments       = allAttachments,
            };

            ViewBag.Denominations = denominations;
            return View(vm);
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(DestructionAuthorizationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var authorization = await _authorizationRepository.FindById(id.Value);
            if (authorization == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.Denominations = await _stockService.GetAllDenominations();
            ViewBag.Branches      = branches?.ToList() ?? new();
            return View(authorization);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateDestructionAuthorizationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var authorization = await _authorizationRepository.FindById(dto.Id);
                if (authorization == null) return NotFound();

                var existingActive = await _authorizationRepository.FindActiveByBranch(dto.BranchId, excludeId: dto.Id);
                if (existingActive != null)
                    return BadRequest(new { message = $"This branch already has another active authorization ({existingActive.ReferenceNumber}, status {existingActive.CurrentStatusName}). It must reach a final status before this one can move to that branch." });

                var denominationIds = dto.Denominations.Select(d => d.DenominationId).ToList();
                var (availableBags, bagsError) = await GetAvailableCancelledBagsAsync(dto.BranchId, denominationIds);
                if (bagsError != null)
                    return BadRequest(new { message = bagsError });

                var allDenominations = await _stockService.GetAllDenominations();
                foreach (var denom in dto.Denominations)
                {
                    var requestedBags = int.TryParse(denom.NumberOfBags, out var bags) ? bags : 0;
                    var available     = availableBags.GetValueOrDefault(denom.DenominationId, 0);
                    if (requestedBags > available)
                    {
                        var denomName = allDenominations?.FirstOrDefault(d => d.Id == denom.DenominationId)?.Name ?? $"ID {denom.DenominationId}";
                        return BadRequest(new { message = $"Number of bags for {denomName} exceeds the {available:N0} bag(s) currently available in the Cancelled category for this branch." });
                    }
                }

                authorization.ReferenceNumber = dto.ReferenceNumber;
                authorization.BranchId        = dto.BranchId;
                authorization.Date            = dto.Date;
                authorization.DestructionAuthorizationDenominations = dto.Denominations.Select(d => new DestructionAuthorizationDenomination
                {
                    DestructionAuthorizationId = dto.Id,
                    DenominationId             = d.DenominationId,
                    NumberOfBags               = d.NumberOfBags,
                    Amount                     = d.Amount
                }).ToList();

                await _authorizationRepository.UpdateWithChildren(authorization);
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
            var authorization = await _authorizationRepository.FindById(id);
            if (authorization == null)
            {
                TempData["Error"] = "Destruction authorization not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.DestructionAuthorization,
                    FromStatusId   = authorization.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.DestructionAuthorization,
                        username);

                    var missingRequired = transition.AttachmentRequirements
                        .Where(r => r.IsRequired && !(Request.Form.Files[$"attachments_{r.Id}"]?.Length > 0))
                        .Select(r => r.AttachmentName)
                        .ToList();

                    if (missingRequired.Any())
                    {
                        TempData["Error"] = $"Missing required attachments: {string.Join(", ", missingRequired)}";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var executed = await _workflowService.ExecuteTransition(
                    entityType:      StatusConstants.Entity.DestructionAuthorization,
                    entityId:        id,
                    currentStatusId: authorization.CurrentStatusId,
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

        private async Task<List<long>> HandleAttachmentUploads(
            List<TransitionAttachmentRequirementDto> requirements,
            int workflowTransitionId,
            int entityId,
            string entityType,
            string username)
        {
            var uploaded = new List<long>();
            var uploadsPath = Path.Combine(FileStorageConstants.UploadsRoot, "destruction-authorization", entityId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var formatMap = new Dictionary<string, List<string>>
            {
                { "excel", new List<string> { "xls", "xlsx" } },
                { "word",  new List<string> { "doc", "docx" } },
                { "docx",  new List<string> { "doc", "docx" } },
                { "jpg",   new List<string> { "jpg", "jpeg" } },
                { "jpeg",  new List<string> { "jpg", "jpeg" } },
            };

            foreach (var req in requirements)
            {
                var file = Request.Form.Files[$"attachments_{req.Id}"];
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                var allowed = req.AllowedFormats.Split(',').Select(f => f.Trim().ToLower()).ToList();
                bool valid = allowed.Any(f => formatMap.TryGetValue(f, out var mapped) ? mapped.Contains(ext) : f == ext);
                if (!valid) continue;

                var uniqueName = $"{req.Id}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileNameWithoutExtension(file.FileName)}.{ext}";
                var fullPath   = Path.Combine(uploadsPath, uniqueName);
                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var attachment = new Attachment
                {
                    FileName                          = file.FileName,
                    FilePath                          = Path.Combine("uploads", "destruction-authorization", entityId.ToString(), uniqueName),
                    FileType                          = file.ContentType,
                    FileSize                          = file.Length,
                    WorkflowTransitionId              = workflowTransitionId,
                    TransitionAttachmentRequirementId = req.Id,
                    EntityId                          = entityId.ToString(),
                    EntityType                        = entityType,
                    IsDeleted                         = false,
                };
                await _attachmentRepository.Create(attachment);
                uploaded.Add(attachment.Id);
            }

            return uploaded;
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
