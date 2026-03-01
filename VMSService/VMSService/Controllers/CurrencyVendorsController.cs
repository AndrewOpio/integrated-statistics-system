using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Helpers;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CurrencyVendorsController(
        ICurrencyVendorRepository _currencyVendorRepository,
        IVendorTypeRepository _vendorTypeRepository,
        IMapper _mapper,
        ILogger<CurrencyVendorsController> _logger) : Controller
    {
        [Breadcrumb("Currency Vendors", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: CurrencyVendors
        public async Task<IActionResult> Index()
        {
            try
            {
                var currencyVendors = (await _currencyVendorRepository.FindAll()).Where(c => c.IsDeleted ==  false);
                var model = _mapper.Map<List<CurrencyVendorViewModel>>(currencyVendors);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currency vendors");
                TempData["Error"] = "An error occurred while retrieving currency vendors. Please try again.";
                return View(new List<CurrencyVendorViewModel>());
            }
        }

        // GET: CurrencyVendors/Details/5
        [Breadcrumb("Details", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _currencyVendorRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var currencyVendor = await _currencyVendorRepository.FindById(id.Value);
                if (currencyVendor == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CurrencyVendorViewModel>(currencyVendor);
                model.CountryList = CountriesHelper.GetCountries();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currency vendor with id {CurrencyVendorId}", id);
                TempData["Error"] = "An error occurred while retrieving the currency vendor. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CurrencyVendors/Create
        [Breadcrumb("Create", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var vendorTypes = await _vendorTypeRepository.FindAll();
                var model = new CurrencyVendorViewModel
                {
                    CountryList = CountriesHelper.GetCountries(),
                    VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) vendorTypes)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vendor types for create");
                TempData["Error"] = "An error occurred while loading vendor types. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CurrencyVendors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CurrencyVendorViewModel model)
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
                
                var vendorTypes = await _vendorTypeRepository.FindAll();
                model.CountryList = CountriesHelper.GetCountries();
                model.VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) vendorTypes);
                return View(model);
            }

            try
            {
                var currencyVendor = _mapper.Map<CurrencyVendor>(model);
                bool isCreated = await _currencyVendorRepository.Create(currencyVendor);

                if (isCreated)
                {
                    TempData["Success"] = "Currency vendor created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create currency vendor. Please try again.";
                    _logger.LogError("Error creating currency vendor");
                    ModelState.AddModelError("", "Failed to create currency vendor. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating currency vendor");
                ModelState.AddModelError("", "Failed to create currency vendor. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var types = await _vendorTypeRepository.FindAll();
            model.CountryList = CountriesHelper.GetCountries();
            model.VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) types);
            return View(model);
        }

        // GET: CurrencyVendors/Edit/5
        [Breadcrumb("Edit", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _currencyVendorRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var currencyVendor = await _currencyVendorRepository.FindById(id.Value);
                if (currencyVendor == null)
                {
                    return NotFound();
                }

                var vendorTypes = await _vendorTypeRepository.FindAll();
                var model = _mapper.Map<CurrencyVendorViewModel>(currencyVendor);
                model.CountryList = CountriesHelper.GetCountries();
                model.VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) vendorTypes);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currency vendor for edit with id {CurrencyVendorId}", id);
                TempData["Error"] = "An error occurred while retrieving the currency vendor. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CurrencyVendors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CurrencyVendorViewModel model)
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
                
                var vendorTypes = await _vendorTypeRepository.FindAll();
                model.CountryList = CountriesHelper.GetCountries();
                model.VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) vendorTypes);
                return View(model);
            }

            try
            {
                var currencyVendor = await _currencyVendorRepository.FindById(id);
                if (currencyVendor == null)
                {
                    return NotFound();
                }

                currencyVendor.Name = model.Name;
                currencyVendor.Description = model.Description;
                currencyVendor.Country = model.Country;
                currencyVendor.VendorTypeId = model.VendorTypeId;

                bool isUpdated = await _currencyVendorRepository.Update(currencyVendor);
                if (isUpdated)
                {
                    TempData["Success"] = "Currency vendor updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update currency vendor. Please try again.";
                    ModelState.AddModelError("", "Failed to update currency vendor. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating currency vendor with id {CurrencyVendorId}", id);
                ModelState.AddModelError("", "Failed to update currency vendor. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var types = await _vendorTypeRepository.FindAll();
            model.CountryList = CountriesHelper.GetCountries();
            model.VendorTypeList = VendorTypesHelper.GetVendorTypesAsSelectList((List<VendorType>) types);
            return View(model);
        }

        // GET: CurrencyVendors/Delete/5
        [Breadcrumb("Delete", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _currencyVendorRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var currencyVendor = await _currencyVendorRepository.FindById(id.Value);
                if (currencyVendor == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CurrencyVendorViewModel>(currencyVendor);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving currency vendor for deletion with id {CurrencyVendorId}", id);
                TempData["Error"] = "An error occurred while retrieving the currency vendor. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CurrencyVendors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CurrencyVendorsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _currencyVendorRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var currencyVendor = await _currencyVendorRepository.FindById(id);
                if (currencyVendor != null)
                {
                    currencyVendor.IsDeleted = true;
                    bool isDeleted = await _currencyVendorRepository.Update(currencyVendor);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Currency vendor deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete currency vendor. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting currency vendor with id {CurrencyVendorId}", id);
                TempData["Error"] = "An error occurred while deleting the currency vendor. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}