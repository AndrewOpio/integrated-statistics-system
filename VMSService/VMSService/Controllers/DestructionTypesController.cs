using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.DestructionTypes;
using VMSService.Core.Repositories.DestructionTypes;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DestructionTypesController(
        IDestructionTypeRepository _destructionTypeRepository,
        IMapper _mapper,
        ILogger<DestructionTypesController> _logger) : Controller
    {
        [Breadcrumb("Destruction Types", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var types = await _destructionTypeRepository.FindAll();
                var model = _mapper.Map<List<DestructionTypeViewModel>>(types);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction types");
                TempData["Error"] = "An error occurred while retrieving destruction types. Please try again.";
                return View(new List<DestructionTypeViewModel>());
            }
        }

        [Breadcrumb("Create", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new DestructionTypeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DestructionTypeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var entity = _mapper.Map<DestructionType>(model);
                bool created = await _destructionTypeRepository.Create(entity);

                if (created)
                {
                    TempData["Success"] = "Destruction type created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to create destruction type. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating destruction type");
                ModelState.AddModelError("", "Failed to create destruction type. An unexpected error occurred.");
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _destructionTypeRepository.IsExists(id.Value)) return NotFound();
                var entity = await _destructionTypeRepository.FindById(id.Value);
                if (entity == null) return NotFound();
                return View(_mapper.Map<DestructionTypeViewModel>(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction type {Id} for edit", id);
                TempData["Error"] = "An error occurred while retrieving the destruction type.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, DestructionTypeViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            try
            {
                var entity = await _destructionTypeRepository.FindById(id);
                if (entity == null) return NotFound();

                entity.Name = model.Name;
                entity.Description = model.Description;

                bool updated = await _destructionTypeRepository.Update(entity);
                if (updated)
                {
                    TempData["Success"] = "Destruction type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to update destruction type. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating destruction type {Id}", id);
                ModelState.AddModelError("", "Failed to update destruction type. An unexpected error occurred.");
                TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            }

            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _destructionTypeRepository.IsExists(id.Value)) return NotFound();
                var entity = await _destructionTypeRepository.FindById(id.Value);
                if (entity == null) return NotFound();
                return View(_mapper.Map<DestructionTypeViewModel>(entity));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving destruction type {Id} for deletion", id);
                TempData["Error"] = "An error occurred while retrieving the destruction type.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(DestructionTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var entity = await _destructionTypeRepository.FindById(id);
                if (entity == null) return NotFound();

                entity.IsDeleted = true;
                bool deleted = await _destructionTypeRepository.Update(entity);

                TempData[deleted ? "Success" : "Error"] = deleted
                    ? "Destruction type deleted successfully!"
                    : "Failed to delete destruction type. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting destruction type {Id}", id);
                TempData["Error"] = "An error occurred while deleting the destruction type.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
