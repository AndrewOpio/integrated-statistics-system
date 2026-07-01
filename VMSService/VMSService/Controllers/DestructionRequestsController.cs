using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.DestructionRequests;
using VMSService.Core.Models.Shared;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.DestructionRequestStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Repositories.DestructionLocations;
using VMSService.Core.Repositories.DestructionRequests;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DestructionRequestsController : Controller
    {
        private readonly IDestructionRequestRepository _destructionRequestRepository;
        private readonly IDestructionRequestStatusHistoryRepository _statusHistoryRepository;
        private readonly IStockService _stockService;
        private readonly IWorkflowServiceClient _workflowServiceClient;
        private readonly IWorkflowService _workflowService;
        private readonly IConfigurationsServiceClient _configServiceClient;
        private readonly IStockLedgerRepository _stockLedgerRepository;
        private readonly ICounterPartyTypeRepository _counterPartyTypeRepository;
        private readonly IDestructionAuthorizationRepository _authorizationRepository;
        private readonly IDestructionLocationRepository _locationRepository;
        private readonly IAttachmentRepository _attachmentRepository;
        private readonly IWebHostEnvironment _environment;

        public DestructionRequestsController(
            IDestructionRequestRepository destructionRequestRepository,
            IDestructionRequestStatusHistoryRepository statusHistoryRepository,
            IStockService stockService,
            IWorkflowServiceClient workflowServiceClient,
            IWorkflowService workflowService,
            IConfigurationsServiceClient configServiceClient,
            IStockLedgerRepository stockLedgerRepository,
            ICounterPartyTypeRepository counterPartyTypeRepository,
            IDestructionAuthorizationRepository authorizationRepository,
            IDestructionLocationRepository locationRepository,
            IAttachmentRepository attachmentRepository,
            IWebHostEnvironment environment)
        {
            _destructionRequestRepository = destructionRequestRepository;
            _statusHistoryRepository      = statusHistoryRepository;
            _stockService                 = stockService;
            _workflowServiceClient        = workflowServiceClient;
            _workflowService              = workflowService;
            _configServiceClient          = configServiceClient;
            _stockLedgerRepository        = stockLedgerRepository;
            _counterPartyTypeRepository   = counterPartyTypeRepository;
            _authorizationRepository      = authorizationRepository;
            _locationRepository           = locationRepository;
            _attachmentRepository         = attachmentRepository;
            _environment                  = environment;
        }

        [Breadcrumb("Destruction Requests", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var requests = await _destructionRequestRepository.FindAll();
            return View(requests);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(DestructionRequestsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.DestructionTypes   = await _destructionRequestRepository.GetAllDestructionTypes();
            ViewBag.Denominations      = await _stockService.GetAllDenominations();
            ViewBag.Branches           = branches.ToList();
            ViewBag.Authorizations     = await _authorizationRepository.GetApprovedAuthorizations();
            ViewBag.Locations          = await _locationRepository.FindAll();
            return View();
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(DestructionRequestsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var request = await _destructionRequestRepository.FindById(id.Value);
            if (request == null) return NotFound();

            var denominations = await _stockService.GetAllDenominations();

            var historyEntities = await _statusHistoryRepository.FindByDestructionRequestId(id.Value);
            var statusHistory = new List<StatusHistoryViewModel>();
            foreach (var h in historyEntities.OrderBy(x => x.ChangedAt))
            {
                var entry = new StatusHistoryViewModel
                {
                    OldStatusName = h.OldStatusName,
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
                            var att = await _attachmentRepository.FindById(attachmentId);
                            if (att != null && !att.IsDeleted)
                                attachments.Add(new AttachmentViewModel
                                {
                                    Id           = att.Id,
                                    FileName     = att.FileName,
                                    FilePath     = att.FilePath,
                                    FileType     = att.FileType,
                                    FileSize     = att.FileSize,
                                    UploadedDate = att.CreatedDate,
                                });
                        }
                    }
                    entry.Attachments = attachments;
                }

                statusHistory.Add(entry);
            }

            var availableTransitions = new List<WorkflowTransitionDto>();
            foreach (var roleId in GetUserRoles())
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName     = StatusConstants.Entity.DestructionRequest,
                    CurrentStatusId    = request.CurrentStatusId,
                    AppRole            = roleId,
                });
                if (transitions != null)
                    availableTransitions.AddRange(transitions);
            }

            string branchName = string.Empty;
            if (request.BranchId > 0)
            {
                var branch = await _configServiceClient.GetBouBranchByIdAsync(request.BranchId);
                branchName = branch?.Name ?? "Unknown";
            }

            var allAttachments = (await _attachmentRepository.FindByEntityId(id.Value.ToString()))
                .Where(a => !a.IsDeleted && a.EntityType == StatusConstants.Entity.DestructionRequest)
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

            var vm = new DestructionRequestDetailsViewModel
            {
                Id                            = request.Id,
                ReferenceNumber               = request.ReferenceNumber,
                AuthorizationReferenceNumber  = request.DestructionAuthorization?.ReferenceNumber ?? string.Empty,
                DestructionAuthorizationId    = request.DestructionAuthorizationId,
                DestructionTypeName           = request.DestructionType?.Name ?? string.Empty,
                Date                 = request.Date,
                VerificationOfficer  = request.VerificationOfficer,
                BranchName           = branchName,
                LocationName         = request.DestructionLocation?.Name,
                CurrentStatusId      = request.CurrentStatusId,
                CurrentStatusName    = request.CurrentStatusName,
                CreatedBy            = request.CreatedBy,
                CreatedDate          = request.CreatedDate,
                Denominations        = request.DestructionRequestDenominations ?? [],
                Members              = request.DestructionRequestMembers?.Where(m => !m.IsWitness) ?? [],
                Witnesses            = request.DestructionRequestMembers?.Where(m => m.IsWitness) ?? [],
                AvailableTransitions = availableTransitions,
                StatusHistory        = statusHistory,
                AllAttachments       = allAttachments,
            };

            ViewBag.Denominations = denominations;
            return View(vm);
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(DestructionRequestsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var request = await _destructionRequestRepository.FindById(id.Value);
            if (request == null) return NotFound();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            ViewBag.DestructionTypes = await _destructionRequestRepository.GetAllDestructionTypes();
            ViewBag.Denominations    = await _stockService.GetAllDenominations();
            ViewBag.Branches         = branches.ToList();
            ViewBag.Authorizations   = await _authorizationRepository.GetApprovedAuthorizations();
            ViewBag.Locations        = await _locationRepository.FindAll();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromBody] UpdateDestructionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                var request = await _destructionRequestRepository.FindById(dto.Id);
                if (request == null) return NotFound();

                var editTypes    = await _destructionRequestRepository.GetAllDestructionTypes();
                var editType     = editTypes.FirstOrDefault(t => t.Id == dto.DestructionTypeId);
                bool editOnline  = editType?.Name == StockConstants.DestructionTypes.Online;

                request.DestructionAuthorizationId = editOnline ? null : dto.DestructionAuthorizationId;
                request.DestructionTypeId          = dto.DestructionTypeId;
                request.DestructionLocationId      = dto.DestructionLocationId;
                request.BranchId                   = dto.BranchId;
                request.ReferenceNumber            = dto.ReferenceNumber;
                request.Date                       = dto.Date;
                request.VerificationOfficer        = dto.VerificationOfficer;
                request.DestructionRequestDenominations = dto.Denominations.Select(d => new DestructionRequestDenomination
                {
                    DestructionRequestId = dto.Id,
                    DenominationId       = d.DenominationId,
                    NumberOfBags         = editOnline ? null : d.NumberOfBags,
                    NumberOfNotes        = editOnline ? d.NumberOfNotes : null,
                    Amount               = d.Amount
                }).ToList();
                request.DestructionRequestMembers = dto.Members.Select(m => new DestructionRequestMember
                {
                    DestructionRequestId = dto.Id,
                    IDNumber             = m.IDNumber,
                    FullName             = m.FullName,
                    IsWitness            = m.IsWitness
                }).ToList();

                await _destructionRequestRepository.UpdateWithChildren(request);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDestructionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            try
            {
                int initialStatusId = 0;
                string initialStatusName = StatusConstants.DestructionRequest.Draft;
                try
                {
                    var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionRequest,
                        StatusName         = StatusConstants.DestructionRequest.Draft,
                    });
                    if (draftStatus?.Name != null)
                    {
                        initialStatusId   = draftStatus.Id;
                        initialStatusName = draftStatus.Name;
                    }
                }
                catch { /* WorkflowService may not be configured yet */ }

                // Validate amounts against authorization remaining balances (skip for Online type)
                var destructionTypes = await _destructionRequestRepository.GetAllDestructionTypes();
                var selectedType     = destructionTypes.FirstOrDefault(t => t.Id == dto.DestructionTypeId);
                bool isOnline        = selectedType?.Name == StockConstants.DestructionTypes.Online;

                if (!isOnline)
                {
                    var remaining     = await _authorizationRepository.GetRemainingAmounts(dto.DestructionAuthorizationId ?? 0);
                    var denominations = await _stockService.GetAllDenominations();
                    foreach (var denom in dto.Denominations)
                    {
                        var available = remaining.GetValueOrDefault(denom.DenominationId, 0m);
                        if (denom.Amount > available)
                        {
                            var denomName = denominations.FirstOrDefault(d => d.Id == denom.DenominationId)?.Name ?? $"ID {denom.DenominationId}";
                            return BadRequest(new { message = $"Amount for {denomName} exceeds the remaining authorized amount of {available:N0}." });
                        }
                    }
                }

                var request = new DestructionRequest
                {
                    DestructionAuthorizationId = isOnline ? null : dto.DestructionAuthorizationId,
                    DestructionTypeId          = dto.DestructionTypeId,
                    DestructionLocationId      = dto.DestructionLocationId,
                    BranchId                   = dto.BranchId,
                    ReferenceNumber            = dto.ReferenceNumber,
                    Date                       = dto.Date,
                    VerificationOfficer        = dto.VerificationOfficer,
                    CurrentStatusId            = initialStatusId,
                    CurrentStatusName          = initialStatusName,
                    DestructionRequestDenominations = dto.Denominations.Select(d => new DestructionRequestDenomination
                    {
                        DenominationId = d.DenominationId,
                        NumberOfBags   = isOnline ? null : d.NumberOfBags,
                        NumberOfNotes  = isOnline ? d.NumberOfNotes : null,
                        Amount         = d.Amount
                    }).ToList(),
                    DestructionRequestMembers = dto.Members.Select(m => new DestructionRequestMember
                    {
                        IDNumber  = m.IDNumber,
                        FullName  = m.FullName,
                        IsWitness = m.IsWitness
                    }).ToList()
                };

                await _destructionRequestRepository.Create(request);
                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Index)) });
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
            var request = await _destructionRequestRepository.FindById(id);
            if (request == null)
            {
                TempData["Error"] = "Destruction request not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var username  = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var userRoles = GetUserRoles();

                // Check if this transition requires attachments
                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.DestructionRequest,
                    FromStatusId   = request.CurrentStatusId,
                    ActionName     = actionName,
                });

                List<long>? uploadedAttachmentIds = null;
                if (transition != null && transition.RequiresAttachments && transition.AttachmentRequirements.Any())
                {
                    uploadedAttachmentIds = await HandleAttachmentUploads(
                        transition.AttachmentRequirements.ToList(),
                        transition.Id,
                        id,
                        StatusConstants.Entity.DestructionRequest,
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
                    entityType:      StatusConstants.Entity.DestructionRequest,
                    entityId:        id,
                    currentStatusId: request.CurrentStatusId,
                    actionName:      actionName,
                    username:        username,
                    userRoles:       userRoles,
                    remarks:         remarks,
                    attachmentIds:   uploadedAttachmentIds);

                if (executed)
                {
                    request = await _destructionRequestRepository.FindById(id);
                    var denoms = request.DestructionRequestDenominations ?? [];

                    if (request.CurrentStatusName == StatusConstants.DestructionRequest.Approved)
                    {
                        var stockAmounts = denoms.Select(d => (d.DenominationId, d.Amount)).ToList();
                        await PostStockMovementForDenoms(request, stockAmounts, isIn: false,
                            reference: request.ReferenceNumber, notes: remarks, username: username);
                    }
                    else if (request.CurrentStatusName == StatusConstants.DestructionRequest.ReturnedForDestruction)
                    {
                        var stockAmounts = denoms.Select(d => (d.DenominationId, d.Amount - d.Destroyed)).ToList();
                        await PostStockMovementForDenoms(request, stockAmounts, isIn: false,
                            reference: request.ReferenceNumber, notes: remarks, username: username);
                    }

                    TempData["Success"] = "Action completed successfully.";
                }
                else
                {
                    TempData["Error"] = $"Failed to execute '{actionName}'. You may not have the required role or the action is unavailable.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while executing the action.";
                _ = ex;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _attachmentRepository.FindById(id);
            if (attachment == null || attachment.IsDeleted)
                return NotFound();

            var filePath = Path.Combine(_environment.WebRootPath, attachment.FilePath);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var memory = new MemoryStream();
            await using (var stream = new FileStream(filePath, FileMode.Open))
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
            var uploaded = new List<long>();
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "destruction", entityId.ToString());
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
                    FilePath                          = Path.Combine("uploads", "destruction", entityId.ToString(), uniqueName),
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordDestruction([FromBody] RecordDestructionDto dto)
        {
            var request = await _destructionRequestRepository.FindById(dto.DestructionRequestId);
            if (request == null)
                return NotFound(new { message = "Destruction request not found." });

            if (request.CurrentStatusName != StatusConstants.DestructionRequest.Approved &&
                request.CurrentStatusName != StatusConstants.DestructionRequest.ReturnedForDestruction)
                return BadRequest(new { message = "Destruction can only be recorded on an Approved or Returned For Destruction request." });

            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                var denoms   = request.DestructionRequestDenominations ?? [];

                // Update Destroyed per denomination
                foreach (var input in dto.Denominations)
                {
                    var denom = denoms.FirstOrDefault(d => d.DenominationId == input.DenominationId);
                    if (denom == null) continue;

                    var newDestroyed = denom.Destroyed + input.DestroyedToday;
                    if (newDestroyed > denom.Amount)
                    {
                        var denominations2 = await _stockService.GetAllDenominations();
                        var denomName = denominations2.FirstOrDefault(d => d.Id == input.DenominationId)?.Name ?? $"ID {input.DenominationId}";
                        return BadRequest(new { message = $"Destroyed amount for {denomName} exceeds the approved amount of {denom.Amount:N0}." });
                    }

                    await _destructionRequestRepository.UpdateDenominationDestroyed(denom.Id, newDestroyed);
                }

                // Re-fetch to get updated Destroyed values
                request = await _destructionRequestRepository.FindById(dto.DestructionRequestId);
                denoms  = request.DestructionRequestDenominations ?? [];

                var balance  = denoms.Sum(d => d.Amount - d.Destroyed);
                var userRoles = GetUserRoles();

                // Fetch available transitions so we use the exact action names from WorkflowService
                var transitions = new List<WorkflowTransitionDto>();
                foreach (var roleId in userRoles)
                {
                    var t = await _workflowServiceClient.GetAvailableTransitions(new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName     = StatusConstants.Entity.DestructionRequest,
                        CurrentStatusId    = request.CurrentStatusId,
                        AppRole            = roleId,
                    });
                    if (t != null) transitions.AddRange(t);
                }

                if (balance <= 0)
                {
                    // Everything destroyed — find the transition that leads to Completed
                    var completeAction = transitions
                        .FirstOrDefault(t => t.ToStatus?.Name == StatusConstants.DestructionRequest.PendingCompletion);

                    if (completeAction == null)
                        return BadRequest(new { message = "No 'Pending Completion' transition available for your role from this status." });
    
                    await _workflowService.ExecuteTransition(
                        entityType:      StatusConstants.Entity.DestructionRequest,
                        entityId:        request.Id,
                        currentStatusId: request.CurrentStatusId,
                        actionName:      completeAction.ActionName,
                        username:        username,
                        userRoles:       userRoles,
                        remarks:         dto.Remarks);
                }
                else
                {
                    // Partial — credit balance back to stock and transition to Returned to Storage
                    var returnAction = transitions
                        .FirstOrDefault(t => t.ToStatus?.Name == StatusConstants.DestructionRequest.ReturnedToStorage);

                    if (returnAction == null)
                        return BadRequest(new { message = "No 'Returned To Storage' transition available for your role from this status." });

                    await PostStockMovementForDenoms(
                        request:   request,
                        denoms:    denoms.Select(d => (d.DenominationId, d.Amount - d.Destroyed)).ToList(),
                        isIn:      true,
                        reference: request.ReferenceNumber,
                        notes:     dto.Remarks,
                        username:  username);

                    await _workflowService.ExecuteTransition(
                        entityType:      StatusConstants.Entity.DestructionRequest,
                        entityId:        request.Id,
                        currentStatusId: request.CurrentStatusId,
                        actionName:      returnAction.ActionName,
                        username:        username,
                        userRoles:       userRoles,
                        remarks:         dto.Remarks);
                }

                return Ok(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = request.Id }) });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message });
            }
        }

        private async Task PostStockMovement(DestructionRequest request, bool isIn, string reference, string notes, string username)
        {
            var denomAmounts = (request.DestructionRequestDenominations ?? [])
                .Select(d => (d.DenominationId, d.Amount))
                .ToList();

            await PostStockMovementForDenoms(request, denomAmounts, isIn, reference, notes, username);
        }

        private async Task PostStockMovementForDenoms(DestructionRequest request, List<(int DenominationId, decimal Amount)> denoms, bool isIn, string reference, string notes, string username)
        {
            var categories       = await _stockService.GetAllCategories();
            var category         = categories.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.Cancelled);
            var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Branch);

            if (category == null || counterPartyType == null) return;

            var details = denoms
                .Where(d => d.Amount > 0)
                .Select(d => new StockLedgerDetail
                {
                    DenominationId  = d.DenominationId,
                    NumberOfBoxes   = (int)(d.Amount / 50000),
                    NumberOfBundles = (int)(d.Amount / 1000),
                    NumberOfPieces  = 0,
                    Value           = d.Amount,
                    CategoryId      = category.Id,
                    CreatedBy       = username,
                    CreatedDate     = DateTime.Now,
                    ModifiedBy      = username,
                    ModifiedDate    = DateTime.Now,
                }).ToList();

            if (!details.Any()) return;

            var ledger = new StockLedger
            {
                ReferenceCode      = reference,
                StoreId            = request.BranchId,
                IsIn               = isIn,
                CounterPartyId     = request.BranchId,
                CounterPartyTypeId = counterPartyType.Id,
                Notes              = notes,
                CreatedBy          = username,
                CreatedDate        = DateTime.Now,
                ModifiedBy         = username,
                ModifiedDate       = DateTime.Now,
                StockLedgerDetails = details,
            };

            await _stockLedgerRepository.Create(ledger);
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