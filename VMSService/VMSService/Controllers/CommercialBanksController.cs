using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.CommercialBanks;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CommercialBanksController(
        ICommercialBankRepository _commercialBankRepository,
        IMapper _mapper,
        ILogger<CommercialBanksController> _logger) : Controller
    {
        [Breadcrumb("Commercial Banks", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: CommercialBanks
        public async Task<IActionResult> Index()
        {
            try
            {
                var commercialBanks = (await _commercialBankRepository.FindAll());
                var model = _mapper.Map<List<CommercialBankViewModel>>(commercialBanks);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial banks");
                TempData["Error"] = "An error occurred while retrieving commercial banks. Please try again.";
                return View(new List<CommercialBankViewModel>());
            }
        }

        // GET: CommercialBanks/Details/5
        [Breadcrumb("Details", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var commercialBank = await _commercialBankRepository.FindById(id.Value);
                bool exists = commercialBank != null && !commercialBank.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (commercialBank == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankViewModel>(commercialBank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank with id {CommercialBankId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CommercialBanks/Create
        [Breadcrumb("Create", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new CommercialBankViewModel());
        }

        // POST: CommercialBanks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CommercialBankViewModel model)
        {
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
                var commercialBank = _mapper.Map<CommercialBank>(model);
                bool isCreated = await _commercialBankRepository.Create(commercialBank);

                if (isCreated)
                {
                    TempData["Success"] = "Commercial bank created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create commercial bank. Please try again.";
                    _logger.LogError("Error creating commercial bank");
                    ModelState.AddModelError("", "Failed to create commercial bank. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commercial bank");
                ModelState.AddModelError("", "Failed to create commercial bank. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: CommercialBanks/Edit/5
        [Breadcrumb("Edit", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var commercialBank = await _commercialBankRepository.FindById(id.Value);
                bool exists = commercialBank != null && !commercialBank.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (commercialBank == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankViewModel>(commercialBank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank for edit with id {CommercialBankId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CommercialBanks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CommercialBankViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

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
                var commercialBank = await _commercialBankRepository.FindById(id);
                if (commercialBank == null)
                {
                    return NotFound();
                }

                // Compare to check if changes were made
                var oldCommercialBank = _mapper.Map<CommercialBankViewModel>(commercialBank);
                if (oldCommercialBank.Name == model.Name &&
                    oldCommercialBank.Code == model.Code)
                {
                    TempData["Error"] = "No changes made.";
                    return View(model);
                }

                commercialBank.Name = model.Name;
                commercialBank.Code = model.Code;

                bool isUpdated = await _commercialBankRepository.Update(commercialBank);
                if (isUpdated)
                {
                    TempData["Success"] = "Commercial bank updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update commercial bank. Please try again.";
                    ModelState.AddModelError("", "Failed to update commercial bank. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commercial bank with id {CommercialBankId}", id);
                ModelState.AddModelError("", "Failed to update commercial bank. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: CommercialBanks/Delete/5
        [Breadcrumb("Delete", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _commercialBankRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBank = await _commercialBankRepository.FindById(id.Value);
                if (commercialBank == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CommercialBankViewModel>(commercialBank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank for deletion with id {CommercialBankId}", id);
                TempData["Error"] = "An error occurred while retrieving the commercial bank. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CommercialBanks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CommercialBanksController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _commercialBankRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var commercialBank = await _commercialBankRepository.FindById(id);
                if (commercialBank != null)
                {
                    commercialBank.IsDeleted = true;
                    bool isDeleted = await _commercialBankRepository.Update(commercialBank);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Commercial bank deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete commercial bank. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commercial bank with id {CommercialBankId}", id);
                TempData["Error"] = "An error occurred while deleting the commercial bank. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}