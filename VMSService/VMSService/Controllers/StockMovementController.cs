using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Azure;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.StockMovement;
using VMSService.Core.Repositories.Logistics;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Services.StockPositions;
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
    public class StockMovementController : Controller
    {
        private readonly ILogger<StockMovementController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IStockMovementStatusHistoryRepository _statusHistoryRepository;
        private readonly IStockMovementDiscrepancyRepository _discrepancyRepository;
        private readonly IWorkflowService _workflowService;
        private readonly IStockService _stockService;
        private readonly IStockPositionService _stockPositionService;
        private readonly ILogisticsRepository _logisticsRepository;
        private readonly IConfigurationsServiceClient _configServiceClient;
        private readonly IWorkflowServiceClient _workflowServiceClient;

        public StockMovementController(
            ILogger<StockMovementController> logger,
            IHttpClientFactory httpClientFactory,
            IStockMovementRepository stockMovementRepository,
            IStockMovementStatusHistoryRepository statusHistoryRepository,
            IStockMovementDiscrepancyRepository discrepancyRepository,
            IWorkflowService workflowService,
            IStockService stockService,
            IStockPositionService stockPositionService,
            ILogisticsRepository logisticsRepository,
            IConfigurationsServiceClient configServiceClient,
            IWorkflowServiceClient workflowServiceClient)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _stockMovementRepository = stockMovementRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _discrepancyRepository = discrepancyRepository;
            _workflowService = workflowService;
            _stockService = stockService;
            _stockPositionService = stockPositionService;
            _logisticsRepository = logisticsRepository;
            _configServiceClient = configServiceClient;
            _workflowServiceClient = workflowServiceClient;
        }
        [Breadcrumb("Stock Movement", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userRolez = User.FindAll("appRole")
                .ToList()
                ;
            var userRoles = User.FindAll("appRole")
               .Where(c => c.Value.StartsWith("CAS:"))
               .Select(c => c.Value.Split(":")[1])
               .ToList();

            var branches = await _configServiceClient.GetAllBouBranchesAsync();

            var branchNames = branches.ToDictionary(b => b.Name, b => b.Id);

            var userBranchIds = userRoles
                .Where(role => branchNames.ContainsKey(role))
                .Select(role => branchNames[role])
                .ToList();

            if (userBranchIds.Any())
                _logger.LogError("No UserBranchId found");

            var movements = await _stockMovementRepository.FindByIdsAsync(userBranchIds);

            StatusTypeDto draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Draft,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            var filteredMovements = movements
                .Where(m => m.CurrentStatusId != draftStatus.Id || m.CreatedBy == currentUserId)
                .ToList();

            var movmt = await _stockMovementRepository.FindAllAsync();

            return View(movmt);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(StockMovementController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var denominations = await _stockService.GetAllDenominations();
            var categories = await _stockService.GetAllCategories();

            // Get user roles
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith("VMS:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            // If user is a Branch Manager, filter branches to only their branch
            if (userRoles.Contains(SystemPolicy.Roles.BranchManager) ||
                userRoles.Contains(SystemPolicy.Roles.AssistantBranchManager))
            {
                // Extract branch names from user roles (everything after "VMS:")
                // Branch managers have roles like "VMS:Kampala Branch", "VMS:Mbarara Branch", etc.
                var userBranchNames = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .Where(role => branches.Any(b => b.Name == role))
                    .ToList();

                // If they have branches assigned, filter to those; otherwise show no branches
                // branches = userBranchNames.Any() 
                //     ? branches.Where(b => userBranchNames.Contains(b.Name)).ToList()
                //     : new List<Branch>();

                if (userBranchNames.Any())
                {
                    branches = branches.Where(b => userBranchNames.Contains(b.Name)).ToList();
                }
            }

            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;
            ViewBag.UserRoles = userRoles;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockMovementCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null)
                return BadRequest(new { message = "Invalid request body." });

            // Get the "Draft" status
            StatusTypeDto draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Draft,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });
            if (draftStatus == null)
            {
                return BadRequest(new { message = "Draft status not found." });
            }

            if (model.MovementType == StockConstants.StockMovementTypes.BranchToBranch)
            {
                var categories = await _stockService.GetAllCategories();

                var filteredDenoms = model.DenominationRows?
                    .Where(x => x.DenominationId > 0 && (x.NumBagsBoxes.HasValue || x.NumBundles.HasValue || x.SubTotalValue > 0))
                    .ToList() ?? new List<StockMovementDenominationViewModel>();

                var movement = new StockMovement
                {
                    MovementType = model.MovementType,
                    Date = model.BranchDate ?? DateTime.Now,
                    ReferenceNumber = model.BranchReferenceNumber,
                    DispatchingBranchId = model.DispatchingBranch,
                    ReceivingBranchId = model.ReceivingBranch,
                    TotalBagsBoxes = model.BranchTotalBagsBoxes,
                    GrandTotalValue = model.BranchGrandTotalValue,
                    CurrentStatusId = draftStatus.Id,
                    Denominations = filteredDenoms.Select(x => {
                        return new StockMovementDenomination
                        {
                            DenominationId = x.DenominationId,
                            NumBagsBoxes = x.NumBagsBoxes,
                            NumBundles = x.NumBundles,
                            CategoryId = x.CategoryId,
                            ContainerType = x.ContainerType,
                            SubTotalValue = x.SubTotalValue,
                            Categorization = x.Categorization,
                            BoxNumberRanges = x.BoxNumberRanges?
                                .Where(r =>
                                    !string.IsNullOrWhiteSpace(r.Prefix) ||
                                    !string.IsNullOrWhiteSpace(r.Start) ||
                                    !string.IsNullOrWhiteSpace(r.End))
                                .Select(r => new StockMovementDenominationBoxNumberRange
                                {
                                    Prefix = r.Prefix,
                                    Start = r.Start,
                                    End = r.End
                                }).ToList()
                        };
                    }).ToList()
                };

                await _stockMovementRepository.CreateAsync(movement);
                return Json(new { redirectUrl = Url.Action("Index") });
            }
            else if (model.MovementType == StockConstants.StockMovementTypes.BufferToOperational ||
                     model.MovementType == StockConstants.StockMovementTypes.OperationalToBuffer)
            {
                var category = await _stockService.GetAllCategories();
                var newCategory = category.FirstOrDefault(c => c.Name == "New");

                var filteredDenoms = model.InternalDenominationRows?
                    .Where(x => x.DenominationId > 0 && (
                        x.NumBoxes.HasValue ||
                        x.SubTotalValue > 0 ||
                        (x.BoxNumberRanges != null && x.BoxNumberRanges.Any(r =>
                            !string.IsNullOrWhiteSpace(r.Prefix) ||
                            !string.IsNullOrWhiteSpace(r.Start) ||
                            !string.IsNullOrWhiteSpace(r.End)))))
                    .ToList() ?? new List<InternalStockMovementDenominationViewModel>();

                var movement = new StockMovement
                {
                    MovementType = model.MovementType,
                    Date = model.InternalDate ?? DateTime.Now,
                    ReferenceNumber = model.InternalReferenceNumber,
                    DispatchingBranchId = model.DispatchingBranch,
                    ReceivingBranchId = model.ReceivingBranch,
                    TotalBagsBoxes = model.InternalTotalBoxes,
                    GrandTotalValue = model.InternalGrandTotalValue,
                    CurrentStatusId = draftStatus.Id,
                    Denominations = filteredDenoms.Select(x => new StockMovementDenomination
                    {
                        DenominationId = x.DenominationId,
                        NumBagsBoxes = x.NumBoxes,
                        CategoryId = newCategory?.Id,
                        ContainerType = x.ContainerType ?? "Box",
                        SubTotalValue = x.SubTotalValue,
                        BoxNumberRanges = x.BoxNumberRanges?
                            .Where(r =>
                                !string.IsNullOrWhiteSpace(r.Prefix) ||
                                !string.IsNullOrWhiteSpace(r.Start) ||
                                !string.IsNullOrWhiteSpace(r.End))
                            .Select(r => new StockMovementDenominationBoxNumberRange
                            {
                                Prefix = r.Prefix,
                                Start = r.Start,
                                End = r.End
                            }).ToList()
                    }).ToList()
                };

                await _stockMovementRepository.CreateAsync(movement);
                return Json(new { redirectUrl = Url.Action("Index") });
            }

            return BadRequest(new { message = "Invalid movement type." });
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(StockMovementController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var movement = await _stockMovementRepository.FindByIdAsync(id);
            if (movement == null)
            {
                return NotFound();
            }

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var denominations = await _stockService.GetAllDenominations();
            var categories = await _stockService.GetAllCategories();
            var commercialBanks = await _configServiceClient.GetAllCommercialBankAsync();

            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;
            ViewBag.CommercialBanks = commercialBanks;

            // Load status history
            var statusHistory = await _statusHistoryRepository.FindByStockMovementId(id);

            // Get available transitions for current user
            var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

            var availableTransitions = new List<Core.Models.StockMovement.WorkflowTransitionDto>();

            StatusTypeDto approvedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Approved,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            // If internal movement and it's already approved, show no transitions
            if ((movement.MovementType?.ToLower() == StockConstants.StockMovementTypes.BufferToOperational ||
                movement.MovementType?.ToLower() == StockConstants.StockMovementTypes.OperationalToBuffer) &&
                movement.CurrentStatusId == approvedStatus.Id)
            {
                availableTransitions = new List<Core.Models.StockMovement.WorkflowTransitionDto>();
            }
            else
            {
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowServiceClient.GetAvailableTransitionsAsync(
                        StatusConstants.Entity.StockMovement,
                        movement.CurrentStatusId ?? 0, 
                        userRole);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions.Select(t => new Core.Models.StockMovement.WorkflowTransitionDto
                        {
                            ActionName = t.ActionName,
                            DisplayLabel = t.DisplayLabel,
                            RequiresComments = t.RequiresComments,
                            ButtonClass = t.ButtonClass
                        }));
                    }
                }
            }

            ViewBag.StatusHistory = statusHistory;
            ViewBag.AvailableTransitions = availableTransitions.Distinct().ToList();

            // Load discrepancies if any exist
            var discrepancies = await _discrepancyRepository.GetByStockMovementIdAsync(id);
            var currentDiscrepancy = discrepancies.FirstOrDefault();
            ViewBag.Discrepancies = discrepancies;
            ViewBag.CurrentDiscrepancy = currentDiscrepancy;

            StatusTypeDto pendingDiscrepancyStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovementDiscrepancy.PendingDiscrepancy,
                EntityTypeName = StatusConstants.Entity.StockMovementDiscrepancy
            });

            ViewBag.HasMismatches = discrepancies.Any(d => d.CurrentStatusId == pendingDiscrepancyStatus.Id);

            StatusTypeDto approvedLogisticsStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.Logistics.Approved,
                EntityTypeName = StatusConstants.Entity.Logistics
            });

            // Get latest 20 approved logistics forms
            var logisticsList = await _logisticsRepository.GetAllAsync();
            var approvedLogistics = logisticsList
                .Where(l => l.CurrentStatusId == approvedLogisticsStatus.Id)
                .OrderByDescending(l => l.Date)
                .Take(20)
                .ToList();
            ViewBag.LogisticsList = approvedLogistics;

            // Get the attached logistics form if it exists
            if (movement.LogisticsId.HasValue)
            {
                var attachedLogistics = await _logisticsRepository.GetByIdAsync(movement.LogisticsId.Value);
                ViewBag.AttachedLogistics = attachedLogistics;
            }

            // Get available discrepancy transitions from workflow system
            var discrepancyTransitions = new List<Core.Models.StockMovement.WorkflowTransitionDto>();
            if (currentDiscrepancy != null)
            {
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowServiceClient.GetAvailableTransitionsAsync(
                        StatusConstants.Entity.StockMovementDiscrepancy,
                        currentDiscrepancy.CurrentStatusId,
                        userRole);
                    if (transitions != null)
                    {
                        discrepancyTransitions.AddRange(transitions.Select(t => new Core.Models.StockMovement.WorkflowTransitionDto
                        {
                            ActionName = t.ActionName,
                            DisplayLabel = t.DisplayLabel,
                            RequiresComments = t.RequiresComments,
                            ButtonClass = t.ButtonClass
                        }));
                    }
                }
            }
            ViewBag.DiscrepancyTransitions = discrepancyTransitions.Distinct().ToList();

            return View(movement);
        }


        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(StockMovementController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id)
        {
            var movement = await _stockMovementRepository.FindByIdAsync(id);
            if (movement == null)
            {
                return NotFound();
            }

            // Only allow editing if the movement is in Draft or Returned status
            StatusTypeDto draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Draft,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            StatusTypeDto returnedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Returned,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            if (movement.CurrentStatusId != draftStatus?.Id && movement.CurrentStatusId != returnedStatus?.Id)
            {
                TempData["Error"] = "Only draft or returned stock movements can be edited.";
                return RedirectToAction("Details", new { id });
            }

            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            var denominations = await _stockService.GetAllDenominations();
            var categories = await _stockService.GetAllCategories();

            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;

            // Map denominations to ViewModel
            var denominationViewModels = new List<dynamic>();
            if (movement.Denominations != null && movement.Denominations.Any())
            {
                foreach (var denom in movement.Denominations)
                {
                    var boxRanges = denom.BoxNumberRanges?.Select(br => new
                    {
                        Prefix = br.Prefix,
                        Start = br.Start,
                        End = br.End
                    }).ToList();

                    denominationViewModels.Add(new
                    {
                        DenominationId = denom.DenominationId,
                        NumBagsBoxes = denom.NumBagsBoxes,
                        NumBundles = denom.NumBundles,
                        CategoryId = denom.CategoryId,
                        Categorization = denom.Categorization,
                        ContainerType = denom.ContainerType,
                        SubTotalValue = denom.SubTotalValue,
                        BoxNumberRanges = boxRanges
                    });
                }
            }

            ViewBag.DenominationViewModels = denominationViewModels;
            return View(movement);
        }


        [HttpPost]
        [Breadcrumb("Edit", FromController = typeof(StockMovementController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, [FromBody] StockMovementCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var movement = await _stockMovementRepository.FindByIdForEditAsync(id);
            if (movement == null)
            {
                return BadRequest(new { message = "Stock movement not found." });
            }

            // Only allow editing if the movement is in Draft or Returned status
            StatusTypeDto draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Draft,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            StatusTypeDto returnedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Returned,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });
            if (movement.CurrentStatusId != draftStatus?.Id && movement.CurrentStatusId != returnedStatus?.Id)
            {
                return BadRequest(new { message = "Only draft or returned stock movements can be edited." });
            }

            if (movement.MovementType == StockConstants.StockMovementTypes.BranchToBranch)
            {
                var categories = await _stockService.GetAllCategories();

                var filteredDenoms = model.DenominationRows?
                    .Where(x => x.DenominationId > 0 && (x.NumBagsBoxes.HasValue || x.NumBundles.HasValue || x.SubTotalValue > 0))
                    .ToList();

                // Update movement properties
                movement.Date = model.BranchDate ?? DateTime.Now;
                movement.ReferenceNumber = model.BranchReferenceNumber;
                movement.DispatchingBranchId = model.DispatchingBranch;
                movement.ReceivingBranchId = model.ReceivingBranch;
                movement.TotalBagsBoxes = model.BranchTotalBagsBoxes;
                movement.GrandTotalValue = model.BranchGrandTotalValue;

                // Update denominations - clear existing and add new ones
                if (movement.Denominations != null)
                {
                    movement.Denominations.Clear();
                }
                else
                {
                    movement.Denominations = new List<StockMovementDenomination>();
                }

                if (filteredDenoms != null)
                {
                    foreach (var x in filteredDenoms)
                    {
                        movement.Denominations.Add(new StockMovementDenomination
                        {
                            DenominationId = x.DenominationId,
                            NumBagsBoxes = x.NumBagsBoxes,
                            NumBundles = x.NumBundles,
                            CategoryId = x.CategoryId,
                            ContainerType = x.ContainerType,
                            SubTotalValue = x.SubTotalValue,
                            Categorization = x.Categorization,
                            BoxNumberRanges = x.BoxNumberRanges?
                                .Where(r =>
                                    !string.IsNullOrWhiteSpace(r.Prefix) ||
                                    !string.IsNullOrWhiteSpace(r.Start) ||
                                    !string.IsNullOrWhiteSpace(r.End))
                                .Select(r => new StockMovementDenominationBoxNumberRange
                                {
                                    Prefix = r.Prefix,
                                    Start = r.Start,
                                    End = r.End
                                }).ToList()
                        });
                    }
                }

                await _stockMovementRepository.SaveAsync();
                return Json(new { redirectUrl = Url.Action("Details", new { id }) });
            }
            else if (movement.MovementType == StockConstants.StockMovementTypes.BufferToOperational || movement.MovementType == StockConstants.StockMovementTypes.OperationalToBuffer)
            {
                var filteredDenoms = model.InternalDenominationRows?
                    .Where(x => x.DenominationId > 0 && (
                        x.NumBoxes.HasValue ||
                        x.SubTotalValue > 0 ||
                        (x.BoxNumberRanges != null && x.BoxNumberRanges.Any(r =>
                            !string.IsNullOrWhiteSpace(r.Prefix) ||
                            !string.IsNullOrWhiteSpace(r.Start) ||
                            !string.IsNullOrWhiteSpace(r.End)))))
                    .ToList();

                var categories = await _stockService.GetAllCategories();
                var newCategory = categories.FirstOrDefault(c => c.Name == "New");

                // Update movement properties
                movement.Date = model.InternalDate ?? DateTime.Now;
                movement.ReferenceNumber = model.InternalReferenceNumber;
                movement.DispatchingBranchId = model.DispatchingBranch;
                movement.ReceivingBranchId = model.ReceivingBranch;
                movement.TotalBagsBoxes = model.InternalTotalBoxes;
                movement.GrandTotalValue = model.InternalGrandTotalValue;

                // Update denominations - clear existing and add new ones
                if (movement.Denominations != null)
                {
                    movement.Denominations.Clear();
                }
                else
                {
                    movement.Denominations = new List<StockMovementDenomination>();
                }

                if (filteredDenoms != null)
                {
                    foreach (var x in filteredDenoms)
                    {
                        movement.Denominations.Add(new StockMovementDenomination
                        {
                            DenominationId = x.DenominationId,
                            NumBagsBoxes = x.NumBoxes,
                            CategoryId = newCategory?.Id,
                            ContainerType = x.ContainerType ?? "Box",
                            SubTotalValue = x.SubTotalValue,
                            BoxNumberRanges = x.BoxNumberRanges?
                                .Where(r =>
                                    !string.IsNullOrWhiteSpace(r.Prefix) ||
                                    !string.IsNullOrWhiteSpace(r.Start) ||
                                    !string.IsNullOrWhiteSpace(r.End))
                                .Select(r => new StockMovementDenominationBoxNumberRange
                                {
                                    Prefix = r.Prefix,
                                    Start = r.Start,
                                    End = r.End
                                }).ToList()
                        });
                    }
                }

                await _stockMovementRepository.SaveAsync();
                return Json(new { redirectUrl = Url.Action("Details", new { id }) });
            }

            return BadRequest(new { message = "Invalid movement type." });
        }


        [HttpPost]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks, int? logisticsId = null)
        {
            var movement = await _stockMovementRepository.FindByIdAsync(id);
            if (movement == null)
                return NotFound();

            var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

            StatusTypeDto pendingReceiptStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.PendingReceipt,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            // Check for discrepancies if received denominations exist
            if (movement?.Denominations != null && movement?.CurrentStatusId == pendingReceiptStatus.Id)
            {
                var sent = movement.Denominations
                    .Where(d => d.DenominationType == "Sent" || d.DenominationType == null)
                    .ToList();
                var received = movement.Denominations
                    .Where(d => d.DenominationType == "Received")
                    .ToList();

                if (received.Any())
                {
                    // Get denomination IDs that were sent vs received
                    var sentDenomIds = sent.Select(d => d.DenominationId).Distinct().ToList();
                    var receivedDenomIds = received.Select(d => d.DenominationId).Distinct().ToList();

                    // Find mismatches
                    var missingDenoms = sentDenomIds.Except(receivedDenomIds).ToList();      // Sent but not received
                    var extraDenoms = receivedDenomIds.Except(sentDenomIds).ToList();        // Received but not sent

                    StatusTypeDto pendingDiscrepancyStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        StatusName = StatusConstants.StockMovementDiscrepancy.PendingDiscrepancy,
                        EntityTypeName = StatusConstants.Entity.StockMovementDiscrepancy
                    });

                    // Handle denomination mismatches
                    if (missingDenoms.Any() || extraDenoms.Any())
                    {

                        var discrepancy = new StockMovementDiscrepancy
                        {
                            StockMovementId = id,
                            CurrentStatusId = pendingDiscrepancyStatus.Id
                        };
                        await _discrepancyRepository.AddAsync(discrepancy);

                    }
                    else
                    {
                        var matchingDenomIds = sentDenomIds.Intersect(receivedDenomIds).ToList();

                        foreach (var denomId in matchingDenomIds)
                        {
                            var sentDenom = sent.FirstOrDefault(d => d.DenominationId == denomId);
                            var receivedDenom = received.FirstOrDefault(d => d.DenominationId == denomId);

                            if (sentDenom != null && receivedDenom != null)
                            {
                                var sentValue = sentDenom.SubTotalValue;
                                var receivedValue = receivedDenom.SubTotalValue;
                                var valueDifference = receivedValue - sentValue;

                                if (valueDifference != 0)
                                {
                                    var discrepancy = new StockMovementDiscrepancy
                                    {
                                        StockMovementId = id,
                                        CurrentStatusId = pendingDiscrepancyStatus.Id
                                    };
                                    await _discrepancyRepository.AddAsync(discrepancy);
                                }
                            }
                        }
                    }
                }
            }

            var success = await _workflowService.ExecuteTransition(
                StatusConstants.Entity.StockMovement,
                id,
                movement.CurrentStatusId ?? 0,
                actionName,
                User.Identity?.Name ?? "unknown",
                userRoles,
                remarks
            );

            if (!success)
                return BadRequest(new { message = "Failed to execute action." });

            // Get the updated movement with new status
            var updatedMovement = await _stockMovementRepository.FindByIdAsync(id);

            // Update logistics ID if provided
            if (logisticsId.HasValue && logisticsId.Value > 0)
            {
                updatedMovement.LogisticsId = logisticsId.Value;
                await _stockMovementRepository.UpdateAsync(updatedMovement);
            }

            // Update stock position if transitioning to Dispatched or Received
            if (updatedMovement != null && updatedMovement.CurrentStatusId.HasValue)
            {
                var statusName = (await _workflowServiceClient.FindStatusById(updatedMovement.CurrentStatusId.Value))?.Name;
                if (statusName == StatusConstants.StockMovement.Dispatched ||
                    statusName == StatusConstants.StockMovement.Received)
                {
                    await _stockPositionService.UpdatePositionAsync(
                        StatusConstants.Entity.StockMovement,
                        statusName,
                        updatedMovement);
                }
            }

            return RedirectToAction(nameof(Details), new { id = movement.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteDiscrepancyAction(int id, string actionName, string remarks)
        {
            try
            {
                var movement = await _stockMovementRepository.FindByIdAsync(id);
                if (movement == null)
                    return NotFound();

                var discrepancies = await _discrepancyRepository.GetByStockMovementIdAsync(id);
                var discrepancy = discrepancies.FirstOrDefault();
                if (discrepancy == null)
                    return BadRequest(new { message = "No discrepancy found for this movement." });

                // Get user roles
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                // Look up the valid transition using workflow transition repository
                var transition = await _workflowServiceClient.FindByTransitionKey(new GetTransitionByKeyRequestDto
                {
                    EntityTypeName = StatusConstants.Entity.StockMovementDiscrepancy,
                    FromStatusId = discrepancy.CurrentStatusId,
                    ActionName = actionName
                });

                if (transition == null)
                {
                    _logger.LogWarning("No valid transition found for discrepancy action {Action}", actionName);
                    return BadRequest(new { message = "Invalid action." });
                }

                // Verify user has permission
                if (!userRoles.Contains(transition.RequiredRole))
                {
                    _logger.LogWarning("User attempted discrepancy action {Action} without proper role {RequiredRole}", actionName, transition.RequiredRole);
                    return BadRequest(new { message = "Unauthorized action." });
                }

                // Get the new status
                var newStatusType = await _workflowServiceClient.FindStatusById(transition.ToStatusId);
                if (newStatusType == null)
                {
                    _logger.LogWarning("New status not found for transition");
                    return BadRequest(new { message = "Invalid state transition." });
                }

                var oldStatusType = await _workflowServiceClient.FindStatusById(discrepancy.CurrentStatusId);

                // Use workflow service to execute the transition
                var userId = User.Identity?.Name ?? "unknown";
                var success = await _workflowService.ExecuteDiscrepancyTransition(
                    discrepancy.Id,
                    discrepancy.CurrentStatusId,
                    oldStatusType.Name,
                    newStatusType.Id,
                    newStatusType.Name,
                    userId,
                    remarks);

                if (!success)
                {
                    _logger.LogWarning("Failed to execute discrepancy transition for discrepancy {DiscrepancyId}", discrepancy.Id);
                    return BadRequest(new { message = "Failed to process discrepancy transition." });
                }

                // Update stock position if transitioning to Resolved
                if (newStatusType.Name == StatusConstants.StockMovementDiscrepancy.DiscrepancyResolved)
                {
                    await _stockPositionService.UpdatePositionAsync(
                        StatusConstants.Entity.StockMovementDiscrepancy,
                        newStatusType.Name,
                        discrepancy);
                }

                TempData["SuccessMessage"] = $"Discrepancy status updated to {newStatusType.Name}.";
                return RedirectToAction(nameof(Details), new { id = movement.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing discrepancy action for movement {MovementId}", id);
                return BadRequest(new { message = "An error occurred while processing the discrepancy action." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReceivedStock(int id, [FromForm] string receivedData)
        {
            try
            {
                var movement = await _stockMovementRepository.FindByIdAsync(id);
                if (movement == null)
                    return NotFound();

                // Parse received data from JSON
                if (string.IsNullOrEmpty(receivedData))
                    return BadRequest(new { message = "No received data provided." });

                var receivedRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ReceivedDenominationDto>>(receivedData);
                if (receivedRows == null || receivedRows.Count == 0)
                    return BadRequest(new { message = "Invalid received data format." });

                // Convert DTO to entity
                var receivedDenominations = receivedRows.Select(row => new StockMovementDenomination
                {
                    DenominationId = row.SentDenominationId,
                    NumBagsBoxes = row.ReceivedBagsBoxes,
                    NumBundles = row.ReceivedBundles,
                    CategoryId = row.CategoryId,
                    SubTotalValue = row.SubTotalValue
                }).ToList();

                // Save through repository
                var success = await _stockMovementRepository.SaveReceivedDenominationsAsync(id, receivedDenominations);

                if (!success)
                    return BadRequest(new { message = "Failed to save received denominations." });

                TempData["SuccessMessage"] = "Received denominations recorded successfully.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving received stock for movement {MovementId}", id);
                return BadRequest(new { message = "An error occurred while saving received denominations." });
            }
        }
    }
}