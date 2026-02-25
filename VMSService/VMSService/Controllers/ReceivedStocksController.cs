using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.StatusTypes;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    public class ReceivedStocksController(
        IReceivedStockRepository _receivedStockRepository,
        IExPrintMintRepository _exPrintMintRepository,
        IStatusTypeRepository _statusTypeRepository,
        IMapper _mapper,
        ILogger<ReceivedStocksController> _logger) : Controller
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
                StatusType status = await _statusTypeRepository.FindByName(StatusConstants.ReceivedStock.PendingConfirmation, StatusConstants.Category.ReceivedStock);
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
    }
}