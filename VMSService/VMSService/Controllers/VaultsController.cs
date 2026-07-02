using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Vaults;
using VMSService.Core.Repository.Vaults;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class VaultsController(
        IVaultRepository _vaultRepository,
        IConfigurationsServiceClient _configServiceClient,
        IMapper _mapper,
        ILogger<VaultsController> _logger) : Controller
    {
        [Breadcrumb("Vaults", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var vaults = await _vaultRepository.FindAll();
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                var model = _mapper.Map<List<VaultViewModel>>(vaults);
                var branchDict = branches.ToDictionary(b => b.Id, b => b.Name);
                foreach (var item in model)
                {
                    if (branchDict.TryGetValue(item.BranchId, out var branchName))
                        item.BranchName = branchName;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vaults");
                TempData["Error"] = "An error occurred while retrieving vaults. Please try again.";
                return View(new List<VaultViewModel>());
            }
        }

        [Breadcrumb("Create", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new VaultViewModel();
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for create vault");
                TempData["Error"] = "An error occurred while loading form data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(VaultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateBranchList(model);
                return View(model);
            }

            try
            {
                var vault = _mapper.Map<Vault>(model);
                bool isCreated = await _vaultRepository.Create(vault);

                if (isCreated)
                {
                    TempData["Success"] = "Vault created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create vault. Please try again.";
                ModelState.AddModelError("", "Failed to create vault. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vault");
                ModelState.AddModelError("", "Failed to create vault. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _vaultRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var vault = await _vaultRepository.FindById(id.Value);
                if (vault == null) return NotFound();

                var model = _mapper.Map<VaultViewModel>(vault);
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vault for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the vault. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, VaultViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateBranchList(model);
                return View(model);
            }

            try
            {
                var vault = await _vaultRepository.FindById(id);
                if (vault == null) return NotFound();

                vault.Name = model.Name;
                vault.BranchId = model.BranchId;
                vault.IsActive = model.IsActive;

                bool isUpdated = await _vaultRepository.Update(vault);
                if (isUpdated)
                {
                    TempData["Success"] = "Vault updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update vault. Please try again.";
                ModelState.AddModelError("", "Failed to update vault. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vault with id {Id}", id);
                ModelState.AddModelError("", "Failed to update vault. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _vaultRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var vault = await _vaultRepository.FindById(id.Value);
                if (vault == null) return NotFound();

                var model = _mapper.Map<VaultViewModel>(vault);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vault for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the vault. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(VaultsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool isDeleted = await _vaultRepository.DeleteVaultAsync(id);
                TempData[isDeleted ? "Success" : "Error"] = isDeleted
                    ? "Vault deleted successfully!"
                    : "Failed to delete vault. Please try again.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vault with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the vault. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBranchList(VaultViewModel model)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BranchList = branches
                .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
                .ToList();
        }
    }
}
