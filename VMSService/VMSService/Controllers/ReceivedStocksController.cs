using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Models.WorkflowTransitions;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ReceivedStocksController(
        IReceivedStockRepository _receivedStockRepository,
        IExPrintMintRepository _exPrintMintRepository,
        IStatusTypeRepository _statusTypeRepository,
        IMapper _mapper,
        ILogger<ReceivedStocksController> _logger,
        IWorkflowService _workflowService,
        IReceivedStockStatusHistoryRepository _statusHistoryRepository,
        IStockLedgerRepository _stockLedgerRepository,
        IStockLedgerDetailRepository _stockLedgerDetailRepository) : Controller
    {
        [Breadcrumb("Received Stock", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: ReceivedStocks
        public async Task<IActionResult> Index()
        {
            try
            {
                var receivedStocks = (await _receivedStockRepository.FindAll());
                var model = _mapper.Map<List<ReceivedStockViewModel>>(receivedStocks);
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
                ViewBag.ExPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindAll());
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
            ViewBag.ExPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindAll());

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

                ViewBag.ExPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindAll());

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

            ViewBag.ExPrintMints = _mapper.Map<List<ExPrintMintViewModel>>(await _exPrintMintRepository.FindAll());

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
                            var updatedReceivedStock = await _receivedStockRepository.FindById(id);

                            // Create StockLedger entry
                            var stockLedger = new StockLedger
                            {
                                ReferenceCode = receivedStock.ExPrintMint.ExPrintMintReference,
                                IsIn = true,
                                CounterPartyId = 1,
                                CategoryId = 1, //Remove 
                                Notes = remarks,
                                CreatedBy = username ?? "unknown",
                                CreatedDate = DateTime.Now,
                                ModifiedBy = username ?? "unknown",
                                ModifiedDate = DateTime.Now,
                                StockLedgerDetails = new List<StockLedgerDetail>()
                            };

                            StockLedger createdLedgerRecord = await _stockLedgerRepository.Create(stockLedger);

                            if (createdLedgerRecord != null)
                            {
                                // Create StockLedgerDetail entries
                                for (int i = 0; i < updatedReceivedStock.NumberOfPieces; i++)
                                {
                                    var ledgerDetail = new StockLedgerDetail
                                    {
                                        StockLedgerId = createdLedgerRecord.Id,
                                        DenominationId = receivedStock.ExPrintMint.DenominationId,
                                        Count = receivedStock.NumberOfPieces,
                                        Values = (int) receivedStock.Value,
                                        CreatedBy = username ?? "unknown",
                                        ModifiedBy = username ?? "unknown",
                                        CreatedDate = DateTime.Now,
                                        ModifiedDate = DateTime.Now
                                    };

                                    await _stockLedgerDetailRepository.Create(ledgerDetail);
                                }

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
                        TempData["Success"] = "Stock Position Updated Successfuly and Notification Sent!";
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
    }
}