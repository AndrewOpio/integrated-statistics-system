using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.StockMovement;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Repositories.WorkflowTransitions;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StockMovementController(
        ILogger<StockMovementController> _logger,
        IHttpClientFactory _httpClientFactory,
        IStockMovementRepository _stockMovementRepository,
        IStockMovementStatusHistoryRepository _statusHistoryRepository,
        IStockMovementDiscrepancyRepository _discrepancyRepository,
        IStatusTypeRepository _statusTypeRepository,
        IWorkflowTransitionRepository _workflowTransitionRepository,
        IWorkflowService _workflowService,
        IStockService _stockService,
        IStockPositionService _stockPositionService) : Controller
    {
        private const string StockServiceBaseUrl = "https://localhost:7158";
        private const string StockServiceTokenEndpoint = "/api/token";
        private const string StockServiceBranchesEndpoint = "/api/branches/get-all";
        private const string StockServiceUsername = "vms";
        private const string StockServicePassword = "12345";

        [Breadcrumb("Stock Movement", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            var movements = await _stockMovementRepository.FindAllAsync();
            return View(movements);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(StockMovementController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var client = _httpClientFactory.CreateClient();
            string token = string.Empty;
            string tokenType = "Bearer";
            var branches = new List<Branch>();
            var denominations = new List<Denomination>();
            var categories = new List<Category>();
            try
            {
                // Step 1: Get JWT token
                var tokenPayload = new { clientId = StockServiceUsername, clientSecret = StockServicePassword };
                var tokenContent = new StringContent(JsonSerializer.Serialize(tokenPayload), Encoding.UTF8, "application/json");
                var tokenResponse = await client.PostAsync($"{StockServiceBaseUrl}{StockServiceTokenEndpoint}", tokenContent);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(tokenJson);
                    token = doc.RootElement.GetProperty("accessToken").GetString();
                    if (doc.RootElement.TryGetProperty("tokenType", out var typeProp))
                    {
                        tokenType = typeProp.GetString() ?? "Bearer";
                    }
                }

                // Step 2: Use token to fetch branches, denominations, categories
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, token);
                    // Branches
                    var response = await client.GetAsync($"{StockServiceBaseUrl}{StockServiceBranchesEndpoint}");
                    if (response.IsSuccessStatusCode)
                    {
                        branches = await response.Content.ReadFromJsonAsync<List<Branch>>() ?? new List<Branch>();
                    }
                    // Denominations
                    var denomResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/denominations/get-all");
                    if (denomResponse.IsSuccessStatusCode)
                    {
                        denominations = await denomResponse.Content.ReadFromJsonAsync<List<Denomination>>() ?? new List<Denomination>();
                    }
                    // Categories
                    var catResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/categories/get-all");
                    if (catResponse.IsSuccessStatusCode)
                    {
                        categories = await catResponse.Content.ReadFromJsonAsync<List<Category>>() ?? new List<Category>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data from StockService");
            }
            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;
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
            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.StockMovement.Draft, StatusConstants.Category.StockMovement);
            if (draftStatus == null)
            {
                return BadRequest(new { message = "Draft status not found." });
            }

            if (model.MovementType == "branch")
            {
                var filteredDenoms = model.DenominationRows?
                    .Where(x => x.DenominationId > 0 && (x.NumBagsBoxes.HasValue || x.NumBundles.HasValue || x.SubTotalValue > 0))
                    .ToList();

                var movement = new StockMovement
                {
                    MovementType = model.MovementType,
                    Date = model.BranchDate ?? DateTime.Now,
                    ReferenceNumber = model.BranchReferenceNumber,
                    DispatchingBranchId = model.DispatchingBranch,
                    ReceivingBranchId = model.ReceivingBranch,
                    Categorization = model.Categorization,
                    TotalBagsBoxes = model.BranchTotalBagsBoxes,
                    GrandTotalValue = model.BranchGrandTotalValue,
                    CurrentStatusId = draftStatus.Id,
                    Denominations = filteredDenoms?.Select(x => new StockMovementDenomination
                    {
                        DenominationId = x.DenominationId,
                        NumBagsBoxes = x.NumBagsBoxes,
                        NumBundles = x.NumBundles,
                        CategoryId = x.CategoryId,
                        SubTotalValue = x.SubTotalValue
                    }).ToList()
                };

                await _stockMovementRepository.CreateAsync(movement);
                return Json(new { redirectUrl = Url.Action("Index") });
            }
            else if (model.MovementType == "internal")
            {
                var filteredDenoms = model.InternalDenominationRows?
                    .Where(x => x.DenominationId > 0 && (
                        x.NumBoxes.HasValue ||
                        x.SubTotalValue > 0 ||
                        (x.SerialRanges != null && x.SerialRanges.Any(r =>
                            !string.IsNullOrWhiteSpace(r.Prefix) ||
                            !string.IsNullOrWhiteSpace(r.Start) ||
                            !string.IsNullOrWhiteSpace(r.End)))))
                    .ToList();

                // Debug logging
                if (filteredDenoms != null)
                {
                    foreach (var denom in filteredDenoms)
                    {
                        _logger.LogInformation($"DenominationId: {denom.DenominationId}, NumBoxes: {denom.NumBoxes}, SubTotal: {denom.SubTotalValue}");
                        if (denom.SerialRanges != null)
                        {
                            foreach (var range in denom.SerialRanges)
                            {
                                _logger.LogInformation($"  SerialRange - Prefix: {range.Prefix}, Start: {range.Start}, End: {range.End}");
                            }
                        }
                    }
                }

                var category = await _stockService.GetAllCategories();
                var newCategory = category.FirstOrDefault(c => c.Name == "New");

                var movement = new StockMovement
                {
                    MovementType = model.MovementType,
                    Date = model.InternalDate ?? DateTime.Now,
                    ReferenceNumber = model.InternalReferenceNumber,
                    TotalBagsBoxes = model.InternalTotalBoxes,
                    GrandTotalValue = model.InternalGrandTotalValue,
                    CurrentStatusId = draftStatus.Id,
                    Denominations = filteredDenoms?.Select(x => new StockMovementDenomination
                    {
                        DenominationId = x.DenominationId,
                        NumBagsBoxes = x.NumBoxes,
                        CategoryId = newCategory?.Id,
                        SubTotalValue = x.SubTotalValue,
                        SerialNumberRanges = x.SerialRanges?
                            .Where(r =>
                                !string.IsNullOrWhiteSpace(r.Prefix) ||
                                !string.IsNullOrWhiteSpace(r.Start) ||
                                !string.IsNullOrWhiteSpace(r.End))
                            .Select(r => new StockMovementDenominationSerialNumberRange
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

            var client = _httpClientFactory.CreateClient();
            string token = string.Empty;
            string tokenType = "Bearer";
            var branches = new List<Branch>();
            var denominations = new List<Denomination>();
            var categories = new List<Category>();
            try
            {
                // Step 1: Get JWT token
                var tokenPayload = new { clientId = StockServiceUsername, clientSecret = StockServicePassword };
                var tokenContent = new StringContent(JsonSerializer.Serialize(tokenPayload), Encoding.UTF8, "application/json");
                var tokenResponse = await client.PostAsync($"{StockServiceBaseUrl}{StockServiceTokenEndpoint}", tokenContent);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(tokenJson);
                    token = doc.RootElement.GetProperty("accessToken").GetString();
                    if (doc.RootElement.TryGetProperty("tokenType", out var typeProp))
                    {
                        tokenType = typeProp.GetString() ?? "Bearer";
                    }
                }

                // Step 2: Use token to fetch branches, denominations, categories
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, token);
                    // Branches
                    var response = await client.GetAsync($"{StockServiceBaseUrl}{StockServiceBranchesEndpoint}");
                    if (response.IsSuccessStatusCode)
                    {
                        branches = await response.Content.ReadFromJsonAsync<List<Branch>>() ?? new List<Branch>();
                    }
                    // Denominations
                    var denomResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/denominations/get-all");
                    if (denomResponse.IsSuccessStatusCode)
                    {
                        denominations = await denomResponse.Content.ReadFromJsonAsync<List<Denomination>>() ?? new List<Denomination>();
                    }
                    // Categories
                    var catResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/categories/get-all");
                    if (catResponse.IsSuccessStatusCode)
                    {
                        categories = await catResponse.Content.ReadFromJsonAsync<List<Category>>() ?? new List<Category>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data from StockService");
            }

            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;

            // Load status history
            var statusHistory = await _statusHistoryRepository.FindByStockMovementId(id);

            // Get available transitions for current user
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith("VMS:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            var availableTransitions = new List<WorkflowTransitionDto>();

            // If internal movement and it's already approved, show no transitions
            if (movement.MovementType?.ToLower() == "internal" &&
                movement.CurrentStatus?.Name == StatusConstants.StockMovement.Approved)
            {
                availableTransitions = new List<WorkflowTransitionDto>();
            }
            else
            {
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowService.GetAvailableTransitions(
                        StatusConstants.Entity.StockMovement,
                        movement.CurrentStatusId ?? 0,
                        userRole);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions.Select(t => new WorkflowTransitionDto
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
            ViewBag.HasMismatches = discrepancies.Any(d => d.CurrentStatus?.Name == StatusConstants.StockMovementDiscrepancy.PendingDiscrepancy);

            // Get available discrepancy transitions from workflow system
            var discrepancyTransitions = new List<WorkflowTransitionDto>();
            if (currentDiscrepancy != null)
            {
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowService.GetAvailableTransitions(
                        StatusConstants.Entity.StockMovementDiscrepancy,
                        currentDiscrepancy.CurrentStatusId,
                        userRole);
                    if (transitions != null)
                    {
                        discrepancyTransitions.AddRange(transitions.Select(t => new WorkflowTransitionDto
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
            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.StockMovement.Draft, StatusConstants.Category.StockMovement);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.StockMovement.Returned, StatusConstants.Category.StockMovement);
            if (movement.CurrentStatusId != draftStatus?.Id && movement.CurrentStatusId != returnedStatus?.Id)
            {
                TempData["Error"] = "Only draft or returned stock movements can be edited.";
                return RedirectToAction("Details", new { id });
            }

            var client = _httpClientFactory.CreateClient();
            string token = string.Empty;
            string tokenType = "Bearer";
            var branches = new List<Branch>();
            var denominations = new List<Denomination>();
            var categories = new List<Category>();
            try
            {
                // Step 1: Get JWT token
                var tokenPayload = new { clientId = StockServiceUsername, clientSecret = StockServicePassword };
                var tokenContent = new StringContent(JsonSerializer.Serialize(tokenPayload), Encoding.UTF8, "application/json");
                var tokenResponse = await client.PostAsync($"{StockServiceBaseUrl}{StockServiceTokenEndpoint}", tokenContent);
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(tokenJson);
                    token = doc.RootElement.GetProperty("accessToken").GetString();
                    if (doc.RootElement.TryGetProperty("tokenType", out var typeProp))
                    {
                        tokenType = typeProp.GetString() ?? "Bearer";
                    }
                }

                // Step 2: Use token to fetch branches, denominations, categories
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, token);
                    // Branches
                    var response = await client.GetAsync($"{StockServiceBaseUrl}{StockServiceBranchesEndpoint}");
                    if (response.IsSuccessStatusCode)
                    {
                        branches = await response.Content.ReadFromJsonAsync<List<Branch>>() ?? new List<Branch>();
                    }
                    // Denominations
                    var denomResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/denominations/get-all");
                    if (denomResponse.IsSuccessStatusCode)
                    {
                        denominations = await denomResponse.Content.ReadFromJsonAsync<List<Denomination>>() ?? new List<Denomination>();
                    }
                    // Categories
                    var catResponse = await client.GetAsync($"{StockServiceBaseUrl}/api/categories/get-all");
                    if (catResponse.IsSuccessStatusCode)
                    {
                        categories = await catResponse.Content.ReadFromJsonAsync<List<Category>>() ?? new List<Category>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch data from StockService");
            }
            ViewBag.Branches = branches;
            ViewBag.Denominations = denominations;
            ViewBag.Categories = categories;
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

            var movement = await _stockMovementRepository.FindByIdAsync(id);
            if (movement == null)
            {
                return BadRequest(new { message = "Stock movement not found." });
            }

            // Only allow editing if the movement is in Draft or Returned status
            var draftStatus = await _statusTypeRepository.FindByName(StatusConstants.StockMovement.Draft, StatusConstants.Category.StockMovement);
            var returnedStatus = await _statusTypeRepository.FindByName(StatusConstants.StockMovement.Returned, StatusConstants.Category.StockMovement);
            if (movement.CurrentStatusId != draftStatus?.Id && movement.CurrentStatusId != returnedStatus?.Id)
            {
                return BadRequest(new { message = "Only draft or returned stock movements can be edited." });
            }

            if (movement.MovementType == "branch")
            {
                var filteredDenoms = model.DenominationRows?
                    .Where(x => x.DenominationId > 0 && (x.NumBagsBoxes.HasValue || x.NumBundles.HasValue || x.SubTotalValue > 0))
                    .ToList();

                // Update movement properties
                movement.Date = model.BranchDate ?? DateTime.Now;
                movement.ReferenceNumber = model.BranchReferenceNumber;
                movement.DispatchingBranchId = model.DispatchingBranch;
                movement.ReceivingBranchId = model.ReceivingBranch;
                movement.Categorization = model.Categorization;
                movement.TotalBagsBoxes = model.BranchTotalBagsBoxes;
                movement.GrandTotalValue = model.BranchGrandTotalValue;

                // Update denominations - clear existing and add new ones
                movement.Denominations?.Clear();
                movement.Denominations = filteredDenoms?.Select(x => new StockMovementDenomination
                {
                    DenominationId = x.DenominationId,
                    NumBagsBoxes = x.NumBagsBoxes,
                    NumBundles = x.NumBundles,
                    CategoryId = x.CategoryId,
                    SubTotalValue = x.SubTotalValue
                }).ToList();

                await _stockMovementRepository.UpdateAsync(movement);
                return Json(new { redirectUrl = Url.Action("Details", new { id }) });
            }
            else if (movement.MovementType == "internal")
            {
                var filteredDenoms = model.InternalDenominationRows?
                    .Where(x => x.DenominationId > 0 && (
                        x.NumBoxes.HasValue ||
                        x.SubTotalValue > 0 ||
                        (x.SerialRanges != null && x.SerialRanges.Any(r =>
                            !string.IsNullOrWhiteSpace(r.Prefix) ||
                            !string.IsNullOrWhiteSpace(r.Start) ||
                            !string.IsNullOrWhiteSpace(r.End)))))
                    .ToList();

                // Update movement properties
                movement.Date = model.InternalDate ?? DateTime.Now;
                movement.ReferenceNumber = model.InternalReferenceNumber;
                movement.TotalBagsBoxes = model.InternalTotalBoxes;
                movement.GrandTotalValue = model.InternalGrandTotalValue;

                // Update denominations - clear existing and add new ones
                movement.Denominations?.Clear();
                movement.Denominations = filteredDenoms?.Select(x => new StockMovementDenomination
                {
                    DenominationId = x.DenominationId,
                    NumBagsBoxes = x.NumBoxes,
                    SubTotalValue = x.SubTotalValue,
                    SerialNumberRanges = x.SerialRanges?
                        .Where(r =>
                            !string.IsNullOrWhiteSpace(r.Prefix) ||
                            !string.IsNullOrWhiteSpace(r.Start) ||
                            !string.IsNullOrWhiteSpace(r.End))
                        .Select(r => new StockMovementDenominationSerialNumberRange
                        {
                            Prefix = r.Prefix,
                            Start = r.Start,
                            End = r.End
                        }).ToList()
                }).ToList();

                await _stockMovementRepository.UpdateAsync(movement);
                return Json(new { redirectUrl = Url.Action("Details", new { id }) });
            }

            return BadRequest(new { message = "Invalid movement type." });
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            var movement = await _stockMovementRepository.FindByIdAsync(id);
            if (movement == null)
                return NotFound();

            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith("VMS:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            // Check for discrepancies if received denominations exist
            if (movement?.Denominations != null && movement?.CurrentStatus?.Name == StatusConstants.StockMovement.PendingReceipt)
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

                    var pendingDiscrepancyStatus = await _statusTypeRepository.FindByName(
                                StatusConstants.StockMovementDiscrepancy.PendingDiscrepancy,
                                StatusConstants.Category.StockMovementDiscrepancy);

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
            
            // Update stock position if transitioning to Dispatched or Received
            if (updatedMovement != null && updatedMovement.CurrentStatusId.HasValue)
            {
                var statusName = (await _statusTypeRepository.FindById(updatedMovement.CurrentStatusId.Value))?.Name;
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
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                // Look up the valid transition using workflow transition repository
                var transition = await _workflowTransitionRepository.FindByTransitionKey(
                    StatusConstants.Entity.StockMovementDiscrepancy,
                    discrepancy.CurrentStatusId,
                    actionName);

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
                var newStatusType = await _statusTypeRepository.FindById(transition.ToStatusId);
                if (newStatusType == null)
                {
                    _logger.LogWarning("New status not found for transition");
                    return BadRequest(new { message = "Invalid state transition." });
                }

                // Use workflow service to execute the transition
                var userId = User.Identity?.Name ?? "unknown";
                var success = await _workflowService.ExecuteDiscrepancyTransition(
                    discrepancy.Id,
                    discrepancy.CurrentStatusId,
                    newStatusType.Id,
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