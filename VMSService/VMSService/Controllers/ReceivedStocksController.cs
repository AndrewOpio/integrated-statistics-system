using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintDetails;
using VMSService.Core.Models.ReceivedStocksDetails;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStocksDetails;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.NewStockMovementLogs;
using VMSService.Core.Repositories.PackingListRangeDetails;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.SubCategories;
using ConfigurationsService.Client;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ReceivedStocksController(
        IReceivedStockRepository _receivedStockRepository,
        IReceivedStockDetailsRepository _receivedStockDetailsRepository,
        IExPrintMintRepository _exPrintMintRepository,
        IExPrintMintDetailRepository _exPrintMintDetailRepository,
        IMapper _mapper,
        ILogger<ReceivedStocksController> _logger,
        IWorkflowService _workflowService,
        IReceivedStockStatusHistoryRepository _statusHistoryRepository,
        IStockLedgerRepository _stockLedgerRepository,
        IStockLedgerDetailRepository _stockLedgerDetailRepository,
        IDenominationRepository _denominationRepository,
        IBranchRepository _branchRepository,
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        IBranchCategoryRepository _branchCategoryRepository,
        IPackingListRangeRepository _packingListRangeRepository,
        IStockMovementRepository _stockMovementRepository,
        INewStockMovementLogRepository _newStockMovementLogRepository,
        IPackingListDetailsRepository _packingListDetailsRepository,
        IConfigurationsServiceClient _configServiceClient,
        IWorkflowServiceClient _workflowServiceClient,
        ICategoryRepository _categoryRepository,
        ISubCategoryRepository _subCategoryRepository) : Controller
    {
        [Breadcrumb("Received Stock", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: ReceivedStocks
        public async Task<IActionResult> Index()
        {
            try
            {
                var receivedStocks = (await _receivedStockRepository.FindAll());
                var model = _mapper.Map<List<ReceivedStockViewModel>>(receivedStocks);

                foreach (var receivedStock in model)
                {
                    if (receivedStock.ExPrintMint != null)
                    {
                        receivedStock.ExPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(receivedStock.ExPrintMint.DenominationId));
                        
                        // Add Branch information through ExPrintMint
                        if (receivedStock.ExPrintMint.BranchId > 0)
                        {
                            var branch = await _configServiceClient.GetBouBranchByIdAsync(receivedStock.ExPrintMint.BranchId);

                            if (branch != null)
                            {
                                receivedStock.ExPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                            }
                        }
                    }
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received stocks");
                TempData["Error"] = "An error occurred while retrieving received stocks. Please try again.";
                return View(new List<ReceivedStockViewModel>());
            }
        }

        // GET: ReceivedStocks/Details/5
        [Breadcrumb("Details", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _receivedStockRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var receivedStock = await _receivedStockRepository.FindById(id.Value);
                if (receivedStock == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<ReceivedStockViewModel>(receivedStock);

                // Explicitly map ReceivedStockDetails and load denomination names
                model.Details = _mapper.Map<ICollection<ReceivedStockDetailViewModel>>(receivedStock.ReceivedStockDetails);
                
                // Populate denomination names for each detail
                if (model.Details != null)
                {
                    foreach (var detail in model.Details)
                    {
                        // Load the PackingListRange with its ExPrintMintDetail
                        var packingListRange = await _packingListRangeRepository.FindByIdAsync(detail.PackingListRangeId);
                        if (packingListRange != null)
                        {
                            // Get the ExPrintMintDetail associated with this PackingListRange
                            var exPrintMintDetail = await _exPrintMintDetailRepository.FindById(packingListRange.ExPrintMintDetailId);
                            if (exPrintMintDetail != null)
                            {
                                // Get the Denomination for this ExPrintMintDetail
                                var denomination = await _denominationRepository.FindById(exPrintMintDetail.DenominationId);
                                if (denomination != null)
                                {
                                    detail.DenominationName = denomination.Name;
                                }
                            }
                        }
                    }
                }

                // Get available transitions for current user
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                var availableTransitions = new List<WorkflowTransitionDto>();
                foreach (var userRole in userRoles)
                {
                    var requestData = new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.ReceivedStock,
                        CurrentStatusId = receivedStock.CurrentStatusId,
                        AppRole = userRole,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }

                model.AvailableTransitions = availableTransitions.Distinct().ToList();

                model.StatusHistory = _mapper.Map<ICollection<ReceivedStockStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByReceivedStockId(id ?? 0));

                if (model.ExPrintMint != null)
                {
                    model.ExPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(model.ExPrintMint.DenominationId));
                    
                    // Add Branch information through ExPrintMint
                    if (model.ExPrintMint.BranchId > 0)
                    {
                        var branch = await _branchRepository.FindById(model.ExPrintMint.BranchId);
                        if (branch != null)
                        {
                            model.ExPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received stock with id {ReceivedStockId}", id);
                TempData["Error"] = "An error occurred while retrieving the received stock. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ReceivedStocks/Create
        [Breadcrumb("Create", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var exPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindPendingDeliveries());
                foreach (var exPrintMint in exPrintMints)
                {
                    exPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(exPrintMint.DenominationId));
                    
                    // Load details (denominations) for this ExPrintMint batch
                    var details = await _exPrintMintDetailRepository.FindByExPrintMintId(exPrintMint.Id);
                    if (details != null && details.Any())
                    {
                        // Create a collection of anonymous objects with denomination info for JavaScript
                        exPrintMint.Details = new List<ExPrintMintDetailViewModel>();
                        foreach (var detail in details)
                        {
                            var denomination = await _denominationRepository.FindById(detail.DenominationId);
                            exPrintMint.Details.Add(new ExPrintMintDetailViewModel
                            {
                                Id = detail.Id,
                                ExPrintMintId = detail.ExPrintMintId,
                                DenominationId = detail.DenominationId,
                                DenominationName = denomination?.Name ?? $"Denomination {detail.DenominationId}",
                                NumberOfPieces = detail.NumberOfPieces,
                                Value = detail.Value,
                                NumberOfBoxes = detail.NumberOfBoxes
                            });
                        }
                    }
                    
                    // Add Branch information through ExPrintMint
                    if (exPrintMint.BranchId > 0)
                    {
                        var branch = await _branchRepository.FindById(exPrintMint.BranchId);
                        if (branch != null)
                        {
                            exPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                        }
                    }
                }
                ViewBag.ExPrintMints = exPrintMints;
                
                // Load all PackingListRanges for the "Add new row" dropdown
                // Get ranges and load their denominations
                var allPackingListRanges = await _packingListRangeRepository.FindAllActiveAsync();
                var rangesWithDenominations = new List<dynamic>();
                foreach (var range in allPackingListRanges)
                {
                    var denomination = await _denominationRepository.FindById(range.ExPrintMintDetail.DenominationId);
                    rangesWithDenominations.Add(new 
                    { 
                        range.Id, 
                        DenominationName = denomination?.Name ?? "Unknown",
                    });
                }
                ViewBag.PackingListRanges = rangesWithDenominations;
                
                var model = new ReceivedStockViewModel
                {
                    Details = new List<ReceivedStockDetailViewModel> 
                    { 
                        new ReceivedStockDetailViewModel() 
                    }
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                TempData["Error"] = $"An error occurred. Please try again. \n{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReceivedStocks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(ReceivedStockViewModel model)
        {
            var exPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindPendingDeliveries());
            foreach (var exPrintMint in exPrintMints)
            {
                exPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(exPrintMint.DenominationId));
                
                // Add Branch information through ExPrintMint
                if (exPrintMint.BranchId > 0)
                {
                    var branch = await _branchRepository.FindById(exPrintMint.BranchId);
                    if (branch != null)
                    {
                        exPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                    }
                }
            }
            ViewBag.ExPrintMints = exPrintMints;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                ModelState.AddModelError("", "Please correct the errors and try again.");
                return View(model);
            }

            try
            {
                StatusTypeDto status = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.ReceivedStock.Draft,
                    EntityTypeName = StatusConstants.Entity.ReceivedStock
                });
                model.CurrentStatusId = status.Id;
                model.CurrentStatusName = status.Name;

                ReceivedStock newReceivedStock = _mapper.Map<ReceivedStock>(model);

                newReceivedStock.ReceivedStockDetails = new List<ReceivedStockDetails>();

                foreach (var detailViewModel in model.Details)
                {
                    // Both ExPrintMint and manual rows now submit actual PackingListRangeId
                    if (detailViewModel.PackingListRangeId > 0)
                    {
                        var detailEntity = new ReceivedStockDetails
                        {
                            PackingListRangeId = detailViewModel.PackingListRangeId,
                            ReceivedStartCaseNumber = detailViewModel.ReceivedStartCaseNumber,
                            ReceivedEndCaseNumber = detailViewModel.ReceivedEndCaseNumber,
                            IsDeleted = false
                        };
                        newReceivedStock.ReceivedStockDetails.Add(detailEntity);
                    }
                }


                bool isCreated = await _receivedStockRepository.Create(newReceivedStock);

                if (isCreated)
                {
                    TempData["Success"] = "Received stock created successfully!";
                    return RedirectToAction(nameof(Details), new { id = newReceivedStock.Id });
                }
                else
                {
                    TempData["Error"] = "Failed to create received stock. Please try again.";
                    _logger.LogError("Error creating received stock");
                    ModelState.AddModelError("", "Failed to create received stock. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating received stock");
                ModelState.AddModelError("", "Failed to create received stock. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: ReceivedStocks/Edit/5
        [Breadcrumb("Edit", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _receivedStockRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var receivedStock = await _receivedStockRepository.FindById(id.Value);
                if (receivedStock == null)
                {
                    return NotFound();
                }

                var exPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindPendingDeliveries());
                foreach (var exPrintMint in exPrintMints)
                {
                    exPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(exPrintMint.DenominationId));
                    
                    // Add Branch information through ExPrintMint
                    if (exPrintMint.BranchId > 0)
                    {
                        var branch = await _branchRepository.FindById(exPrintMint.BranchId);
                        if (branch != null)
                        {
                            exPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                        }
                    }
                }
                ViewBag.ExPrintMints = exPrintMints;

                var model = _mapper.Map<ReceivedStockViewModel>(receivedStock);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received stock for edit with id {ReceivedStockId}", id);
                TempData["Error"] = "An error occurred while retrieving the received stock. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReceivedStocks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, ReceivedStockViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var exPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindPendingDeliveries());
            foreach (var exPrintMint in exPrintMints)
            {
                exPrintMint.Denomination = _mapper.Map<DenominationViewModel>(await _denominationRepository.FindById(exPrintMint.DenominationId));
                
                // Add Branch information through ExPrintMint
                if (exPrintMint.BranchId > 0)
                {
                    var branch = await _branchRepository.FindById(exPrintMint.BranchId);
                    if (branch != null)
                    {
                        exPrintMint.Branch = _mapper.Map<BranchViewModel>(branch);
                    }
                }
            }
            ViewBag.ExPrintMints = exPrintMints;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                ModelState.AddModelError("", "Please correct the errors and try again.");

                return View(model);
            }

            try
            {
                var receivedStock = await _receivedStockRepository.FindById(id);
                if (receivedStock == null)
                {
                    return NotFound();
                }

                receivedStock.ExPrintMintId = model.ExPrintMintId;
                receivedStock.DateDelivered = model.DateDelivered;
                //receivedStock.NumberOfPieces = model.NumberOfPieces;
                //receivedStock.NumberOfBoxes = model.NumberOfBoxes;
                //receivedStock.Value = model.Value;
                //receivedStock.BoxNumberPrefix = model.BoxNumberPrefix;
                //receivedStock.BoxNumberRangeStart = model.BoxNumberRangeStart;
                //receivedStock.BoxNumberRangeEnd = model.BoxNumberRangeEnd;
                //receivedStock.SerialNumberPrefix = model.SerialNumberPrefix;
                //receivedStock.SerialNumberRangeStart = model.SerialNumberRangeStart;
                //receivedStock.SerialNumberRangeEnd = model.SerialNumberRangeEnd;

                bool isUpdated = await _receivedStockRepository.Update(receivedStock);
                if (isUpdated)
                {
                    TempData["Success"] = "Received stock updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "Failed to update received stock. Please try again.";
                    ModelState.AddModelError("", "Failed to update received stock. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating received stock with id {ReceivedStockId}", id);
                ModelState.AddModelError("", "Failed to update received stock. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: ReceivedStocks/Delete/5
        [Breadcrumb("Delete", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _receivedStockRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var receivedStock = await _receivedStockRepository.FindById(id.Value);
                if (receivedStock == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<ReceivedStockViewModel>(receivedStock);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving received stock for deletion with id {ReceivedStockId}", id);
                TempData["Error"] = "An error occurred while retrieving the received stock. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: ReceivedStocks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(ReceivedStocksController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _receivedStockRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var receivedStock = await _receivedStockRepository.FindById(id);
                if (receivedStock != null)
                {
                    receivedStock.IsDeleted = true;
                    bool isDeleted = await _receivedStockRepository.Update(receivedStock);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Received stock deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete received stock. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting received stock with id {ReceivedStockId}", id);
                TempData["Error"] = "An error occurred while deleting the received stock. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Add generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var receivedStock = await _receivedStockRepository.FindById(id);
                if (receivedStock == null)
                {
                    TempData["Error"] = "Received Stock not found.";
                    return RedirectToAction(nameof(Index));
                }

                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";
                if (username == null)
                {
                    //_logger.LogWarning("Username claim not found for user executing action {Action} on Received Stock {ReceivedStockId}", actionName, id);
                    TempData["Error"] = "The session ended. Please ";
                    return RedirectToAction(nameof(Details), new { id });
                }

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.ReceivedStock,
                    id,
                    receivedStock.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    if (actionName.Equals("Confirm"))
                    {
                        try
                        {
                            // Step 1: Create StockMovement and get its ID
                            var updatedReceivedStock = await _receivedStockRepository.FindById(id);
                            var createdMovement = await CreateStockMovementFromReceivedStock(updatedReceivedStock, username);

                            if (createdMovement != null)
                            {
                                // Step 2: Create StockLedger using the movement ID, with one StockLedgerDetail
                                // per denomination actually present in the received stock's details.
                                var store = await _branchCategoryRepository
                                    .FindByBranchAndCategoryName(receivedStock.ExPrintMint.BranchId, StockConstants.StoreTypes.New);
                                var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Vendor);
                                var categories = await _categoryRepository.FindAll();
                                var newCategory = categories?.FirstOrDefault(c => c.Name == StockConstants.StoreTypes.New);
                                var subCategories = await _subCategoryRepository.FindAll();
                                var naSubCategory = subCategories?.FirstOrDefault(s => s.Name == StockConstants.SubCategories.NA);

                                var detailsByDenomination = await GroupReceivedStockDetailsByDenomination(updatedReceivedStock);

                                var stockLedgerDetails = new List<StockLedgerDetail>();
                                foreach (var (denominationId, details) in detailsByDenomination)
                                {
                                    var denomination = await _denominationRepository.FindById(denominationId);
                                    int numberOfBoxes = details.Sum(d => d.ReceivedEndCaseNumber - d.ReceivedStartCaseNumber + 1);
                                    int numberOfPieces = numberOfBoxes * (denomination?.NumberOfPiecesInABox ?? 0);

                                    stockLedgerDetails.Add(new StockLedgerDetail
                                    {
                                        CreatedDate = DateTime.Now,
                                        ModifiedDate = DateTime.Now,
                                        CreatedBy = username,
                                        ModifiedBy = username,
                                        DenominationId = denominationId,
                                        NumberOfPieces = numberOfPieces,
                                        NumberOfBoxes = numberOfBoxes,
                                        NumberOfBundles = numberOfBoxes,
                                        Value = denomination != null ? denomination.Value * numberOfPieces : 0,
                                        CategoryId = newCategory?.Id ?? 0,
                                        SubCategoryId = naSubCategory?.Id ?? 0,
                                    });
                                }

                                var stockLedger = new StockLedger
                                {
                                    ReferenceCode = receivedStock.ExPrintMint.ExPrintMintReference,
                                    StockMovementId = createdMovement.Id,
                                    StoreId = store?.Id ?? 0,
                                    IsIn = true,
                                    CounterPartyId = receivedStock.ExPrintMint.CurrencyVendorId,
                                    CounterPartyTypeId = counterPartyType?.Id ?? 0,
                                    Notes = remarks,
                                    CreatedBy = username,
                                    CreatedDate = DateTime.Now,
                                    ModifiedBy = username,
                                    ModifiedDate = DateTime.Now,
                                    StockLedgerDetails = stockLedgerDetails
                                };

                                bool createdLedgerRecord = await _stockLedgerRepository.Create(stockLedger);
                                if (!createdLedgerRecord)
                                    _logger.LogWarning("Failed to create stock ledger for Received Stock {ReceivedStockId}", id);

                                // Step 3: Populate NewStockMovementLogs
                                await PopulateNewStockMovementLogs(createdMovement);
                            }

                            TempData["Success"] = "Stock Position Updated Successfully and Notification Sent!";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing stock records for Received Stock {ReceivedStockId}", id);
                            TempData["Warning"] = "Action confirmed but stock record processing encountered an error. Please verify stock levels.";
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Review completed successfully!";
                    }
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on Received Stock {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<StockMovement?> CreateStockMovementFromReceivedStock(ReceivedStock receivedStock, string username)
        {
            if (receivedStock?.ReceivedStockDetails == null || !receivedStock.ReceivedStockDetails.Any())
            {
                _logger.LogWarning("No received stock details found for received stock {ReceivedStockId}", receivedStock?.Id);
                return null;
            }

            try
            {
                if (receivedStock.ExPrintMint == null)
                {
                    receivedStock.ExPrintMint = await _exPrintMintRepository.FindById(receivedStock.ExPrintMintId);
                }

                if (receivedStock.ExPrintMint == null)
                {
                    _logger.LogWarning("ExPrintMint not found for received stock {ReceivedStockId}", receivedStock.Id);
                    return null;
                }

                var detailsByDenomination = await GroupReceivedStockDetailsByDenomination(receivedStock);
                if (!detailsByDenomination.Any())
                {
                    _logger.LogWarning("No resolvable denominations for received stock {ReceivedStockId}", receivedStock.Id);
                    return null;
                }

                StatusTypeDto receivedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.StockMovement.Received,
                    EntityTypeName = StatusConstants.Entity.StockMovement,
                });

                int statusId = receivedStatus?.Id ?? 1;

                var stockMovement = new StockMovement
                {
                    MovementType = StockConstants.StockMovementTypes.VendorToBranch,
                    Date = DateTime.Now,
                    ReferenceNumber = $"REC-{receivedStock.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                    CurrentStatusId = statusId,
                    DispatchingBranchId = 0,
                    ReceivingBranchId = receivedStock.ExPrintMint.BranchId,
                    CreatedBy = username ?? "system",
                    ModifiedBy = username ?? "system",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Denominations = new List<StockMovementDenomination>()
                };

                // One StockMovementDenomination per denomination actually present in the received
                // stock's details, since a single received stock can span multiple denominations.
                foreach (var (denominationId, details) in detailsByDenomination)
                {
                    var denomination = new StockMovementDenomination
                    {
                        DenominationId = denominationId,
                        BoxNumberRanges = new List<StockMovementDenominationBoxNumberRange>()
                    };

                    foreach (var detail in details)
                    {
                        denomination.BoxNumberRanges.Add(new StockMovementDenominationBoxNumberRange
                        {
                            Start = detail.ReceivedStartCaseNumber.ToString(),
                            End = detail.ReceivedEndCaseNumber.ToString()
                        });
                    }

                    stockMovement.Denominations.Add(denomination);
                }

                var createdMovement = await _stockMovementRepository.CreateAsync(stockMovement);

                if (createdMovement != null)
                    _logger.LogInformation("Stock movement created for received stock {ReceivedStockId}, Movement ID: {MovementId}", receivedStock.Id, createdMovement.Id);

                return createdMovement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateStockMovementFromReceivedStock for received stock {ReceivedStockId}", receivedStock.Id);
                throw;
            }
        }

        // Resolves each detail's actual denomination via PackingListRange -> ExPrintMintDetail
        // (not the single legacy DenominationId on ExPrintMint) and groups details by it, so a
        // received stock spanning multiple denominations is handled per-denomination.
        private async Task<Dictionary<int, List<ReceivedStockDetails>>> GroupReceivedStockDetailsByDenomination(ReceivedStock receivedStock)
        {
            var detailsByDenomination = new Dictionary<int, List<ReceivedStockDetails>>();

            if (receivedStock?.ReceivedStockDetails == null)
                return detailsByDenomination;

            foreach (var detail in receivedStock.ReceivedStockDetails)
            {
                var packingListRange = await _packingListRangeRepository.FindByIdAsync(detail.PackingListRangeId);
                if (packingListRange == null)
                {
                    _logger.LogWarning("PackingListRange {PackingListRangeId} not found for received stock detail {DetailId}", detail.PackingListRangeId, detail.Id);
                    continue;
                }

                var exPrintMintDetail = await _exPrintMintDetailRepository.FindById(packingListRange.ExPrintMintDetailId);
                if (exPrintMintDetail == null)
                {
                    _logger.LogWarning("ExPrintMintDetail {ExPrintMintDetailId} not found for received stock detail {DetailId}", packingListRange.ExPrintMintDetailId, detail.Id);
                    continue;
                }

                if (!detailsByDenomination.TryGetValue(exPrintMintDetail.DenominationId, out var group))
                {
                    group = new List<ReceivedStockDetails>();
                    detailsByDenomination[exPrintMintDetail.DenominationId] = group;
                }

                group.Add(detail);
            }

            return detailsByDenomination;
        }

        private async Task PopulateNewStockMovementLogs(StockMovement movement)
        {
            try
            {
                if (movement?.Denominations == null || !movement.Denominations.Any())
                {
                    _logger.LogWarning("No denominations found for movement {MovementId}", movement?.Id);
                    return;
                }

                var newLogs = new List<NewStockMovementlog>();

                // Iterate through each denomination and its box number ranges
                foreach (var denomination in movement.Denominations)
                {
                    if (denomination.BoxNumberRanges == null || !denomination.BoxNumberRanges.Any())
                    {
                        _logger.LogDebug("No box ranges for denomination {DenominationId}", denomination.DenominationId);
                        continue;
                    }

                    // Process each box number range
                    foreach (var boxRange in denomination.BoxNumberRanges)
                    {
                        if (string.IsNullOrWhiteSpace(boxRange.Start) || string.IsNullOrWhiteSpace(boxRange.End))
                        {
                            _logger.LogDebug("Skipping empty box range for denomination {DenominationId}", denomination.DenominationId);
                            continue;
                        }

                        // Parse the start and end box numbers
                        if (!int.TryParse(boxRange.Start, out int startBox) || !int.TryParse(boxRange.End, out int endBox))
                        {
                            _logger.LogWarning("Failed to parse box range: {Start}-{End}", boxRange.Start, boxRange.End);
                            continue;
                        }

                        // Iterate from start box to end box
                        for (int boxNumber = startBox; boxNumber <= endBox; boxNumber++)
                        {
                            // Find the corresponding PackingListDetails record
                            var packingDetail = await _packingListDetailsRepository.FindByCaseNumberAsync(boxNumber);
                            
                            if (packingDetail != null)
                            {
                                // Create a NewStockMovementlog entry
                                var log = new NewStockMovementlog
                                {
                                    StockMovementId = movement.Id,
                                    PackingListDetailId = packingDetail.Id,
                                    SerialNoteStartNumber = packingDetail.SerialNoteStartNumber,
                                    SerialNoteEndNumber = packingDetail.SerialNoteEndNumber,
                                    AccountingStatusId = null,
                                    CaseFullStatusId = null,
                                    CreatedBy = User.Identity?.Name ?? "system",
                                    ModifiedBy = User.Identity?.Name ?? "system",
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now
                                };

                                newLogs.Add(log);
                            }
                            else
                            {
                                _logger.LogDebug("No PackingListDetails found for case number {CaseNumber}", boxNumber);
                            }
                        }
                    }
                }

                // Batch insert all logs if any were created
                _logger.LogInformation("Creating {LogCount} NewStockMovementLog entries for movement {MovementId}", newLogs.Count, movement.Id);
                
                if (newLogs.Any())
                {
                    try
                    {
                        var result = await _newStockMovementLogRepository.CreateBatchAsync(newLogs);
                        _logger.LogInformation("Batch insert result: {Result} for {Count} logs", result, newLogs.Count);
                    }
                    catch (Exception batchEx)
                    {
                        _logger.LogError(batchEx, "Batch insert failed. Exception: {Message}", batchEx.InnerException?.Message ?? batchEx.Message);
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("No logs were created for movement {MovementId}", movement.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating NewStockMovementLogs for movement {MovementId}", movement?.Id);
                throw;
            }
        }
    }
}