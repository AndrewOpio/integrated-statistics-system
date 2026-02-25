using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Denominations;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    public class DenominationsController(
        IDenominationRepository _denominationRepository,
        IDenominationTypeRepository _denominationTypeRepository,
        IMapper _mapper,
        ILogger<DenominationsController> _logger) : Controller
    {
        [Breadcrumb("Denominations", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: Denominations
        public async Task<IActionResult> Index()
        {
            try
            {
                var denominations = (await _denominationRepository.FindAll()).Where(d => d.IsDeleted == false);
                var model = _mapper.Map<List<DenominationViewModel>>(denominations);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denominations");
                TempData["Error"] = "An error occurred while retrieving denominations. Please try again.";
                return View(new List<DenominationViewModel>());
            }
        }

        // GET: Denominations/Details/5
        [Breadcrumb("Details", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denomination = await _denominationRepository.FindById(id.Value);
                if (denomination == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DenominationViewModel>(denomination);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination with id {DenominationId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Denominations/Create
        [Breadcrumb("Create", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var denominationTypes = await _denominationTypeRepository.FindAll();
                ViewBag.DenominationTypes = denominationTypes.Where(dt => !dt.IsDeleted).ToList();
                return View(new DenominationViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading denomination types for create");
                TempData["Error"] = "An error occurred while loading denomination types. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Denominations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DenominationViewModel model)
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
                
                var denominationTypes = await _denominationTypeRepository.FindAll();
                ViewBag.DenominationTypes = denominationTypes.Where(dt => !dt.IsDeleted).ToList();
                return View(model);
            }

            try
            {
                var denomination = _mapper.Map<Denomination>(model);
                bool isCreated = await _denominationRepository.Create(denomination);

                if (isCreated)
                {
                    TempData["Success"] = "Denomination created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create denomination. Please try again.";
                    _logger.LogError("Error creating denomination");
                    ModelState.AddModelError("", "Failed to create denomination. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination");
                ModelState.AddModelError("", "Failed to create denomination. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var types = await _denominationTypeRepository.FindAll();
            ViewBag.DenominationTypes = types.Where(dt => !dt.IsDeleted).ToList();
            return View(model);
        }

        // GET: Denominations/Edit/5
        [Breadcrumb("Edit", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denomination = await _denominationRepository.FindById(id.Value);
                if (denomination == null)
                {
                    return NotFound();
                }

                var denominationTypes = await _denominationTypeRepository.FindAll();
                ViewBag.DenominationTypes = denominationTypes.Where(dt => !dt.IsDeleted).ToList();

                var model = _mapper.Map<DenominationViewModel>(denomination);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination for edit with id {DenominationId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Denominations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, DenominationViewModel model)
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
                
                var denominationTypes = await _denominationTypeRepository.FindAll();
                ViewBag.DenominationTypes = denominationTypes.Where(dt => !dt.IsDeleted).ToList();
                return View(model);
            }

            try
            {
                var denomination = await _denominationRepository.FindById(id);
                if (denomination == null)
                {
                    return NotFound();
                }

                denomination.Name = model.Name;
                denomination.Value = model.Value;
                denomination.DenominationTypeId = model.DenominationTypeId;
                denomination.NumberOfPiecesInABox = model.NumberOfPiecesInABox;

                bool isUpdated = await _denominationRepository.Update(denomination);
                if (isUpdated)
                {
                    TempData["Success"] = "Denomination updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update denomination. Please try again.";
                    ModelState.AddModelError("", "Failed to update denomination. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination with id {DenominationId}", id);
                ModelState.AddModelError("", "Failed to update denomination. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var types = await _denominationTypeRepository.FindAll();
            ViewBag.DenominationTypes = types.Where(dt => !dt.IsDeleted).ToList();
            return View(model);
        }

        // GET: Denominations/Delete/5
        [Breadcrumb("Delete", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _denominationRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var denomination = await _denominationRepository.FindById(id.Value);
                if (denomination == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DenominationViewModel>(denomination);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination for deletion with id {DenominationId}", id);
                TempData["Error"] = "An error occurred while retrieving the denomination. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Denominations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(DenominationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _denominationRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var denomination = await _denominationRepository.FindById(id);
                if (denomination != null)
                {
                    denomination.IsDeleted = true;
                    bool isDeleted = await _denominationRepository.Update(denomination);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Denomination deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete denomination. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting denomination with id {DenominationId}", id);
                TempData["Error"] = "An error occurred while deleting the denomination. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}