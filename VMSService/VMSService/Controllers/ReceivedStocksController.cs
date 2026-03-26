using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ReceivedStocksController(
        IReceivedStockRepository _receivedStockRepository,
        IExPrintMintRepository _exPrintMintRepository,
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
        IWorkflowServiceClient _workflowServiceClient) : Controller
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

                // Get available transitions for current user
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith("VMS:"))
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
                return View(new ReceivedStockViewModel());
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
                var requestData = new GetStatusByNameRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    StatusName = StatusConstants.ReceivedStock.Draft,
                    EntityTypeName = StatusConstants.Entity.ReceivedStock
                };
                StatusTypeDto status = await _workflowServiceClient.FindStatusByName(requestData);
                model.CurrentStatusId = status.Id;
                model.CurrentStatusName = status.Name;

                ReceivedStock newReceivedStock = _mapper.Map<ReceivedStock>(model);
                //bool isCreated = await _receivedStockRepository.Create(newReceivedStock);
                int newReceivedStockId = await _receivedStockRepository.CreateNew(newReceivedStock);

                if (newReceivedStockId > 0)
                {
                    TempData["Success"] = "Received stock created successfully!";
                    return RedirectToAction(nameof(Details), new { id = newReceivedStockId });
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
                receivedStock.NumberOfPieces = model.NumberOfPieces;
                receivedStock.NumberOfBoxes = model.NumberOfBoxes;
                receivedStock.Value = model.Value;
                receivedStock.BoxNumberPrefix = model.BoxNumberPrefix;
                receivedStock.BoxNumberRangeStart = model.BoxNumberRangeStart;
                receivedStock.BoxNumberRangeEnd = model.BoxNumberRangeEnd;
                receivedStock.SerialNumberPrefix = model.SerialNumberPrefix;
                receivedStock.SerialNumberRangeStart = model.SerialNumberRangeStart;
                receivedStock.SerialNumberRangeEnd = model.SerialNumberRangeEnd;

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
                    .Where(c => c.Value.StartsWith("VMS:"))
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
                                NumberOfPieces = receivedStock.NumberOfPieces,
                                NumberOfBoxes = receivedStock.NumberOfBoxes,
                                Value = receivedStock.Value
                            };

                            List<StockLedgerDetail> stockLedgerDetails = new();
                            stockLedgerDetails.Add(stockLedgerDetail);

                            // Create StockLedger entry
                            StockLedger stockLedger = new()
                            {
                                ReferenceCode = receivedStock.ExPrintMint.ExPrintMintReference,
                                StoreId = store.Id, //Branch - New
                                IsIn = true,
                                CounterPartyId = receivedStock.ExPrintMint.CurrencyVendorId,
                                CounterPartyTypeId = counterPartyType.Id, //Vendor
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

                                _logger.LogInformation("Stock position updated successfully for Received Stock {ReceivedStockId}", id);
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
                        TempData["Success"] = "Stock Position Updated Successfully!";
                    } else
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
    }
}