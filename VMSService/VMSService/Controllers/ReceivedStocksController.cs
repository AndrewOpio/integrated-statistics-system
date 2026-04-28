using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ExPrintMintDetails;
using VMSService.Core.Models.ReceivedStocksDetails;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Models.WorkflowTransitions;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStocksDetails;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.NewStockMovementLogs;
using VMSService.Core.Repositories.PackingListRangeDetails;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ReceivedStocksController(
        IReceivedStockRepository _receivedStockRepository,
        IReceivedStockDetailsRepository _receivedStockDetailsRepository,
        IExPrintMintRepository _exPrintMintRepository,
        IExPrintMintDetailRepository _exPrintMintDetailRepository,
        IStatusTypeRepository _statusTypeRepository,
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
        IPackingListDetailsRepository _packingListDetailsRepository) : Controller
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
                            var branch = await _branchRepository.FindById(receivedStock.ExPrintMint.BranchId);
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
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();

                var availableTransitions = new List<WorkflowTransitionViewModel>();
                foreach (var userRole in userRoles)
                {
                    var transitions = await _workflowService.GetAvailableTransitions(
                        StatusConstants.Entity.ReceivedStock,
                        receivedStock.CurrentStatusId,
                        userRole);
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
                StatusType status = await _statusTypeRepository.FindByName(StatusConstants.ReceivedStock.Draft, StatusConstants.Category.ReceivedStock);
                model.CurrentStatusId = status.Id;

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
                    return RedirectToAction(nameof(Index));
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

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System";
                var userRoles = User.FindAll("appRole")
                    .Where(c => c.Value.StartsWith("VMS:"))
                    .Select(c => c.Value.Split(":")[1])
                    .ToList();
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
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
                    userId,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    if (actionName.Equals("Confirm"))
                    {
                        //Update Stock Position
                        try
                        {
                            //var updatedReceivedStock = await _receivedStockRepository.FindById(id);

                            BranchCategory store = await _branchCategoryRepository
                                .FindByBranchAndCategoryName(receivedStock.ExPrintMint.BranchId, StockConstants.StoreTypes.New);
                            var counterPartyType = await _counterPartyTypeRepository.FindByName(StockConstants.StockCounterPartyType.Vendor);

                            StockLedgerDetail stockLedgerDetail = new()
                            {
                                CreatedDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                CreatedBy = username ?? "unknown",
                                ModifiedBy = username ?? "unknown",
                                DenominationId = receivedStock.ExPrintMint.DenominationId,
                                //NumberOfPieces = receivedStock.NumberOfPieces,
                                //NumberOfBoxes = receivedStock.NumberOfBoxes,
                                //Value = receivedStock.Value
                            };

                            List<StockLedgerDetail> stockLedgerDetails = new();
                            stockLedgerDetails.Add(stockLedgerDetail);

                            // Create StockLedger entry
                            StockLedger stockLedger = new()
                            {
                                ReferenceCode = receivedStock.ExPrintMint.ExPrintMintReference,
                                //StoreId = store.Id, //Branch - New
                                IsIn = true,
                                //CounterPartyId = receivedStock.ExPrintMint.CurrencyVendorId,
                                //CounterPartyTypeId = counterPartyType.Id, //Vendor
                                Notes = remarks,
                                CreatedBy = username ?? "unknown",
                                CreatedDate = DateTime.Now,
                                ModifiedBy = username ?? "unknown",
                                ModifiedDate = DateTime.Now,
                                StockLedgerDetails = stockLedgerDetails
                            };

                            StockLedger createdLedgerRecord = await _stockLedgerRepository.Create(stockLedger);

                            if (createdLedgerRecord != null)
                            {
                                //// Create StockLedgerDetail entries
                                //for (int i = 0; i < receivedStock.NumberOfPieces; i++)
                                //{
                                //    var ledgerDetail = new StockLedgerDetail
                                //    {
                                //        StockLedgerId = createdLedgerRecord.Id,
                                //        DenominationId = receivedStock.ExPrintMint.DenominationId,
                                //        NumberOfPieces = receivedStock.NumberOfPieces,
                                //        NumberOfBoxes = receivedStock.NumberOfBoxes,
                                //        NumberOfBundles = null,
                                //        Value = receivedStock.Value,
                                //        CreatedBy = username ?? "unknown",
                                //        ModifiedBy = username ?? "unknown",
                                //        CreatedDate = DateTime.Now,
                                //        ModifiedDate = DateTime.Now
                                //    };

                                //    await _stockLedgerDetailRepository.Create(ledgerDetail);
                                //}

                                _logger.LogInformation("Stock position updated successfully for Received Stock {ReceivedStockId} and Notification Sent", id);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create stock ledger for Received Stock {ReceivedStockId}", id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error updating stock position for Received Stock {ReceivedStockId}", id);
                            TempData["Warning"] = "Action confirmed but stock position update encountered an error. Please verify stock levels.";
                        }

                        // Create StockMovement from received stock and populate logs
                        try
                        {
                            var updatedReceivedStock = await _receivedStockRepository.FindById(id);
                            await CreateStockMovementFromReceivedStock(updatedReceivedStock, username);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating stock movement from received stock {ReceivedStockId}", id);
                            TempData["Warning"] = "Stock movement creation encountered an error. Please verify manually.";  
                        }

                        TempData["Success"] = "Stock Position Updated Successfully and Notification Sent!";
                    } else
                    {
                        TempData["Success"] = "Review completed successfully and Notification Sent!";
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

        private async Task CreateStockMovementFromReceivedStock(ReceivedStock receivedStock, string username)
        {
            if (receivedStock?.ReceivedStockDetails == null || !receivedStock.ReceivedStockDetails.Any())
            {
                _logger.LogWarning("No received stock details found for received stock {ReceivedStockId}", receivedStock?.Id);
                return;
            }

            try
            {
                // Get the denomination from the ExPrintMint
                if (receivedStock.ExPrintMint == null)
                {
                    receivedStock.ExPrintMint = await _exPrintMintRepository.FindById(receivedStock.ExPrintMintId);
                }

                if (receivedStock.ExPrintMint?.DenominationId == null || receivedStock.ExPrintMint.DenominationId == 0)
                {
                    _logger.LogWarning("Invalid denomination for received stock {ReceivedStockId}", receivedStock.Id);
                    return;
                }

                // Get the Received status directly by name (more efficient than loading all)
                var receivedStatus = await _statusTypeRepository.FindByName("Received", "ReceivedStock");
                int statusId = receivedStatus?.Id ?? 1;

                // Create StockMovement
                var stockMovement = new StockMovement
                {
                    MovementType = "InboundReceived",
                    Date = DateTime.Now,
                    ReferenceNumber = $"REC-{receivedStock.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                    CurrentStatusId = statusId,
                    DispatchingBranchId = receivedStock.ExPrintMint.BranchId,
                    ReceivingBranchId = receivedStock.ExPrintMint.BranchId,
                    CreatedBy = username ?? "system",
                    ModifiedBy = username ?? "system",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Denominations = new List<StockMovementDenomination>()
                };

                // Create denomination entry with box ranges
                var denomination = new StockMovementDenomination
                {
                    DenominationId = receivedStock.ExPrintMint.DenominationId,
                    BoxNumberRanges = new List<StockMovementDenominationBoxNumberRange>()
                };

                // Add all the box ranges from received stock details
                foreach (var detail in receivedStock.ReceivedStockDetails)
                {
                    denomination.BoxNumberRanges.Add(new StockMovementDenominationBoxNumberRange
                    {
                        Start = detail.ReceivedStartCaseNumber.ToString(),
                        End = detail.ReceivedEndCaseNumber.ToString()
                    });
                }

                stockMovement.Denominations.Add(denomination);

                // Save the stock movement
                var createdMovement = await _stockMovementRepository.CreateAsync(stockMovement);

                if (createdMovement != null)
                {
                    _logger.LogInformation("Stock movement created for received stock {ReceivedStockId}, Movement ID: {MovementId}", receivedStock.Id, createdMovement.Id);
                    
                    // Populate NewStockMovementLogs
                    await PopulateNewStockMovementLogs(createdMovement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateStockMovementFromReceivedStock for received stock {ReceivedStockId}", receivedStock.Id);
                throw;
            }
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