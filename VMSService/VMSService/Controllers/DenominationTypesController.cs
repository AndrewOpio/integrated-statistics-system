using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DenominationTypesController(IDenominationTypeRepository _denominationTypeRepository, IMapper _mapper, ILogger<DenominationTypesController> _logger) : Controller
    {
        [Breadcrumb("Denomination Types", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: DenominationTypes
        public async Task<IActionResult> Index()
        {
            try
            {
                var denominationTypes = (await _denominationTypeRepository.FindAll());
                var model = _mapper.Map<List<DenominationTypeViewModel>>(denominationTypes);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination types");
                TempData["Error"] = "An error occurred while retrieving denomination types. Please try again.";
                return View(new List<DenominationTypeViewModel>());
            }
        }

        // GET: DenominationTypes/Details/5
        [Breadcrumb("Details", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denominationType = await _denominationTypeRepository.FindById(id.Value);
                if (denominationType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DenominationTypeViewModel>(denominationType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination type with id {DenominationTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: DenominationTypes/Create
        [Breadcrumb("Create", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new DenominationTypeViewModel());
        }

        // POST: DenominationTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DenominationTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var denominationType = _mapper.Map<DenominationType>(model);
                bool isCreated = await _denominationTypeRepository.Create(denominationType);

                if (isCreated)
                {
                    TempData["Success"] = "Denomination type created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create denomination type. Please try again.";
                    _logger.LogError("Error creating denomination type");
                    ModelState.AddModelError("", "Failed to create denomination type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination type");
                ModelState.AddModelError("", "Failed to create denomination type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: DenominationTypes/Edit/5
        [Breadcrumb("Edit", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denominationType = await _denominationTypeRepository.FindById(id.Value);
                if (denominationType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DenominationTypeViewModel>(denominationType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination type for edit with id {DenominationTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DenominationTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, DenominationTypeViewModel model)
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
                var denominationType = await _denominationTypeRepository.FindById(id);
                if (denominationType == null)
                {
                    return NotFound();
                }

                if (denominationType.Name == model.Name)
                {
                    TempData["Error"] = "No changes were made to the denomination type.";
                    return View(model);
                }

                denominationType.Name = model.Name;

                bool isUpdated = await _denominationTypeRepository.Update(denominationType);
                if (isUpdated)
                {
                    TempData["Success"] = "Denomination type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update denomination type. Please try again.";
                    ModelState.AddModelError("", "Failed to update denomination type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination type with id {DenominationTypeId}", id);
                ModelState.AddModelError("", "Failed to update denomination type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: DenominationTypes/Delete/5
        [Breadcrumb("Delete", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denominationType = await _denominationTypeRepository.FindById(id.Value);
                if (denominationType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DenominationTypeViewModel>(denominationType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination type for deletion with id {DenominationTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DenominationTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(DenominationTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _denominationTypeRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var denominationType = await _denominationTypeRepository.FindById(id);
                if (denominationType != null)
                {
                    denominationType.IsDeleted = true;
                    bool isDeleted = await _denominationTypeRepository.Update(denominationType);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Denomination type deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete denomination type. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting denomination type with id {DenominationTypeId}", id);
                TempData["Error"] = "An error occurred while deleting the denomination type. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}