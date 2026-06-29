using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.VaultSections;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Repository.Vaults;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class VaultSectionsController(
        IVaultSectionRepository _vaultSectionRepository,
        IVaultRepository _vaultRepository,
        IMapper _mapper,
        ILogger<VaultSectionsController> _logger) : Controller
    {
        [Breadcrumb("Vault Sections", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var sections = await _vaultSectionRepository.FindAll();
                var model = _mapper.Map<List<VaultSectionViewModel>>(sections);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vault sections");
                TempData["Error"] = "An error occurred while retrieving vault sections. Please try again.";
                return View(new List<VaultSectionViewModel>());
            }
        }

        [Breadcrumb("Create", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new VaultSectionViewModel();
                await PopulateVaultList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vaults for create vault section");
                TempData["Error"] = "An error occurred while loading form data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(VaultSectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateVaultList(model);
                return View(model);
            }

            try
            {
                var section = _mapper.Map<VaultSection>(model);
                bool isCreated = await _vaultSectionRepository.Create(section);

                if (isCreated)
                {
                    TempData["Success"] = "Vault section created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create vault section. Please try again.";
                ModelState.AddModelError("", "Failed to create vault section. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vault section");
                ModelState.AddModelError("", "Failed to create vault section. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateVaultList(model);
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _vaultSectionRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var section = await _vaultSectionRepository.FindById(id.Value);
                if (section == null) return NotFound();

                var model = _mapper.Map<VaultSectionViewModel>(section);
                await PopulateVaultList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vault section for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the vault section. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, VaultSectionViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateVaultList(model);
                return View(model);
            }

            try
            {
                var section = await _vaultSectionRepository.FindById(id);
                if (section == null) return NotFound();

                section.Name = model.Name;
                section.VaultId = model.VaultId;
                section.IsActive = model.IsActive;

                bool isUpdated = await _vaultSectionRepository.Update(section);
                if (isUpdated)
                {
                    TempData["Success"] = "Vault section updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update vault section. Please try again.";
                ModelState.AddModelError("", "Failed to update vault section. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vault section with id {Id}", id);
                ModelState.AddModelError("", "Failed to update vault section. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateVaultList(model);
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _vaultSectionRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var section = await _vaultSectionRepository.FindById(id.Value);
                if (section == null) return NotFound();

                var model = _mapper.Map<VaultSectionViewModel>(section);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vault section for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the vault section. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(VaultSectionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool isDeleted = await _vaultSectionRepository.DeleteSectionAsync(id);
                TempData[isDeleted ? "Success" : "Error"] = isDeleted
                    ? "Vault section deleted successfully!"
                    : "Failed to delete vault section. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vault section with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the vault section. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateVaultList(VaultSectionViewModel model)
        {
            var vaults = await _vaultRepository.FindAll();
            model.VaultList = vaults
                .Where(v => v.IsActive)
                .Select(v => new SelectListItem(v.Name, v.Id.ToString()))
                .ToList();
        }
    }
}
