using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.DestructionLocations;
using VMSService.Core.Repositories.DestructionLocations;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DestructionLocationsController(
        IDestructionLocationRepository _locationRepository,
        IMapper _mapper,
        ILogger<DestructionLocationsController> _logger) : Controller
    {
        [Breadcrumb("Destruction Locations", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var locations = await _locationRepository.FindAll();
                var model = _mapper.Map<List<DestructionLocationViewModel>>(locations);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction locations");
                TempData["Error"] = "An error occurred while retrieving destruction locations.";
                return View(new List<DestructionLocationViewModel>());
            }
        }

        [Breadcrumb("Create", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new DestructionLocationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DestructionLocationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var entity = _mapper.Map<DestructionLocation>(model);
                bool created = await _locationRepository.Create(entity);

                if (created)
                {
                    TempData["Success"] = "Destruction location created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to create destruction location. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating destruction location");
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _locationRepository.IsExists(id.Value)) return NotFound();
                var entity = await _locationRepository.FindById(id.Value);
                if (entity == null) return NotFound();
                return View(_mapper.Map<DestructionLocationViewModel>(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction location {Id} for edit", id);
                TempData["Error"] = "An error occurred while retrieving the destruction location.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, DestructionLocationViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            try
            {
                var entity = await _locationRepository.FindById(id);
                if (entity == null) return NotFound();

                entity.Name        = model.Name;
                entity.Description = model.Description;

                bool updated = await _locationRepository.Update(entity);
                if (updated)
                {
                    TempData["Success"] = "Destruction location updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to update destruction location. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating destruction location {Id}", id);
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _locationRepository.IsExists(id.Value)) return NotFound();
                var entity = await _locationRepository.FindById(id.Value);
                if (entity == null) return NotFound();
                return View(_mapper.Map<DestructionLocationViewModel>(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction location {Id} for deletion", id);
                TempData["Error"] = "An error occurred while retrieving the destruction location.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(DestructionLocationsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var entity = await _locationRepository.FindById(id);
                if (entity == null) return NotFound();

                entity.IsDeleted = true;
                bool deleted = await _locationRepository.Update(entity);

                TempData[deleted ? "Success" : "Error"] = deleted
                    ? "Destruction location deleted successfully!"
                    : "Failed to delete destruction location. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting destruction location {Id}", id);
                TempData["Error"] = "An error occurred while deleting the destruction location.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
