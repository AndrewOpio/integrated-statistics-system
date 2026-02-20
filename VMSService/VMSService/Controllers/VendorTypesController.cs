using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.VendorTypes;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    public class VendorTypesController(IVendorTypeRepository _vendorTypeRepository, IMapper _mapper, ILogger<VendorTypesController> _logger) : Controller
    {
        [Breadcrumb("Vendor Types", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: VendorTypes
        public async Task<IActionResult> Index()
        {
            try
            {
                var vendorTypes = (await _vendorTypeRepository.FindAll()).Where(v => v.IsDeleted == false);
                var model = _mapper.Map<List<VendorTypeViewModel>>(vendorTypes);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor types");
                TempData["Error"] = "An error occurred while retrieving vendor types. Please try again.";
                return View(new List<VendorTypeViewModel>());
            }
        }

        // GET: VendorTypes/Details/5
        [Breadcrumb("Details", FromController = typeof(VendorTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _vendorTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var vendorType = await _vendorTypeRepository.FindById(id.Value);
                if (vendorType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<VendorTypeViewModel>(vendorType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor type with id {VendorTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the vendor type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VendorTypes/Create
        [Breadcrumb("Create", FromController = typeof(VendorTypesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new VendorTypeViewModel());
        }

        // POST: VendorTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var vendorType = _mapper.Map<VendorType>(model);
                bool isCreated = await _vendorTypeRepository.Create(vendorType);

                if (isCreated)
                {
                    TempData["Success"] = "Vendor type created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create vendor type. Please try again.";
                    _logger.LogError("Error creating vendor type");
                    ModelState.AddModelError("", "Failed to create vendor type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vendor type");
                ModelState.AddModelError("", "Failed to create vendor type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: VendorTypes/Edit/5
        [Breadcrumb("Edit", FromController = typeof(VendorTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _vendorTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var vendorType = await _vendorTypeRepository.FindById(id.Value);
                if (vendorType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<VendorTypeViewModel>(vendorType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor type for edit with id {VendorTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the vendor type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VendorTypeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var vendorType = await _vendorTypeRepository.FindById(id);
                if (vendorType == null)
                {
                    return NotFound();
                }

                vendorType.Name = model.Name;
                vendorType.Description = model.Description;

                bool isUpdated = await _vendorTypeRepository.Update(vendorType);
                if (isUpdated)
                {
                    TempData["Success"] = "Vendor type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update vendor type. Please try again.";
                    ModelState.AddModelError("", "Failed to update vendor type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vendor type with id {VendorTypeId}", id);
                ModelState.AddModelError("", "Failed to update vendor type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: VendorTypes/Delete/5
        [Breadcrumb("Delete", FromController = typeof(VendorTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _vendorTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var vendorType = await _vendorTypeRepository.FindById(id.Value);
                if (vendorType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<VendorTypeViewModel>(vendorType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vendor type for deletion with id {VendorTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the vendor type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VendorTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _vendorTypeRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var vendorType = await _vendorTypeRepository.FindById(id);
                if (vendorType != null)
                {
                    vendorType.IsDeleted = true;
                    bool isDeleted = await _vendorTypeRepository.Update(vendorType);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Vendor type deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete vendor type. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vendor type with id {VendorTypeId}", id);
                TempData["Error"] = "An error occurred while deleting the vendor type. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
