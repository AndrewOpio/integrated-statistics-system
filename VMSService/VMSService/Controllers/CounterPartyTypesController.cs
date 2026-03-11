using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.CounterPartyTypes;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CounterPartyTypesController(
        ICounterPartyTypeRepository _counterPartyTypeRepository,
        IMapper _mapper,
        ILogger<CounterPartyTypesController> _logger) : Controller
    {
        [Breadcrumb("Counter Party Types", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: CounterPartyTypes
        public async Task<IActionResult> Index()
        {
            try
            {
                var counterPartyTypes = (await _counterPartyTypeRepository.FindAll());
                var model = _mapper.Map<List<CounterPartyTypeViewModel>>(counterPartyTypes);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party types");
                TempData["Error"] = "An error occurred while retrieving counter party types. Please try again.";
                return View(new List<CounterPartyTypeViewModel>());
            }
        }

        // GET: CounterPartyTypes/Details/5
        [Breadcrumb("Details", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var counterPartyType = await _counterPartyTypeRepository.FindById(id.Value);
                bool exists = counterPartyType != null && !counterPartyType.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (counterPartyType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CounterPartyTypeViewModel>(counterPartyType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type with id {CounterPartyTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the counter party type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CounterPartyTypes/Create
        [Breadcrumb("Create", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new CounterPartyTypeViewModel());
        }

        // POST: CounterPartyTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CounterPartyTypeViewModel model)
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
                var counterPartyType = _mapper.Map<CounterPartyType>(model);
                bool isCreated = await _counterPartyTypeRepository.Create(counterPartyType);

                if (isCreated)
                {
                    TempData["Success"] = "Counter Party Type created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create counter party type. Please try again.";
                    _logger.LogError("Error creating counter party type");
                    ModelState.AddModelError("", "Failed to create counter party type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating counter party type");
                ModelState.AddModelError("", "Failed to create counter party type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: CounterPartyTypes/Edit/5
        [Breadcrumb("Edit", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var counterPartyType = await _counterPartyTypeRepository.FindById(id.Value);
                bool exists = counterPartyType != null && !counterPartyType.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (counterPartyType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CounterPartyTypeViewModel>(counterPartyType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type for edit with id {CounterPartyTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the counter party type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CounterPartyTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CounterPartyTypeViewModel model)
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
                var counterPartyType = await _counterPartyTypeRepository.FindById(id);
                if (counterPartyType == null)
                {
                    return NotFound();
                }

                //Compare to check if changes were made
                var oldCounterPartyType = _mapper.Map<CounterPartyTypeViewModel>(counterPartyType);
                if (oldCounterPartyType.Name == model.Name)
                {
                    TempData["Error"] = "No changes Made";
                    return View(model);
                }

                counterPartyType.Name = model.Name;

                bool isUpdated = await _counterPartyTypeRepository.Update(counterPartyType);
                if (isUpdated)
                {
                    TempData["Success"] = "Counter Party Type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update counter party type. Please try again.";
                    ModelState.AddModelError("", "Failed to update counter party type. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating counter party type with id {CounterPartyTypeId}", id);
                ModelState.AddModelError("", "Failed to update counter party type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: CounterPartyTypes/Delete/5
        [Breadcrumb("Delete", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _counterPartyTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var counterPartyType = await _counterPartyTypeRepository.FindById(id.Value);
                if (counterPartyType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CounterPartyTypeViewModel>(counterPartyType);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type for deletion with id {CounterPartyTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the counter party type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CounterPartyTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CounterPartyTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _counterPartyTypeRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var counterPartyType = await _counterPartyTypeRepository.FindById(id);
                if (counterPartyType != null)
                {
                    counterPartyType.IsDeleted = true;
                    bool isDeleted = await _counterPartyTypeRepository.Update(counterPartyType);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Counter Party Type deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete counter party type. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting counter party type with id {CounterPartyTypeId}", id);
                TempData["Error"] = "An error occurred while deleting the counter party type. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}