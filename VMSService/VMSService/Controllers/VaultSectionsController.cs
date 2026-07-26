using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.VaultSections;
using VMSService.Core.Models.Vaults;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Repository.Vaults;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class VaultSectionsController(
        IVaultSectionRepository _vaultSectionRepository,
        IVaultRepository _vaultRepository,
        IConfigurationsServiceClient _configServiceClient,
        IMapper _mapper,
        ILogger<VaultSectionsController> _logger) : Controller
    {
        [Breadcrumb("Vault Sections", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var sections = (await _vaultSectionRepository.FindAll()).ToList();
                var model = _mapper.Map<List<VaultSectionViewModel>>(sections);

                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                var branchDict = branches.ToDictionary(b => b.Id, b => b.Name);

                for (int i = 0; i < sections.Count; i++)
                {
                    var branchId = sections[i].Vault?.BranchId;
                    if (branchId.HasValue)
                    {
                        model[i].BranchId = branchId.Value;
                        if (branchDict.TryGetValue(branchId.Value, out var branchName))
                            model[i].BranchName = branchName;
                    }
                }

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
                await PopulateBranchList(model);
                await PopulateAllVaultsWithBranch(model);
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
                await PopulateBranchList(model);
                await PopulateAllVaultsWithBranch(model);
                return View(model);
            }

            try
            {
                var existingSections = await _vaultSectionRepository.FindAll();
                bool isDuplicate = existingSections.Any(s =>
                    s.VaultId == model.VaultId &&
                    string.Equals(s.Name.Trim(), model.Name.Trim(), StringComparison.OrdinalIgnoreCase));

                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(model.Name), "A vault section with this name already exists in the selected vault.");
                    await PopulateBranchList(model);
                    await PopulateAllVaultsWithBranch(model);
                    return View(model);
                }

                var section = _mapper.Map<VaultSection>(model);
                section.Vault = null;
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

            await PopulateBranchList(model);
            await PopulateAllVaultsWithBranch(model);
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

                model.BranchId = section.Vault?.BranchId ?? 0;
                if (model.BranchId > 0)
                {
                    var branches = await _configServiceClient.GetAllBouBranchesAsync();
                    model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                }

                await PopulateVaultList(model, model.BranchId);
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
                await PopulateVaultList(model, model.BranchId);
                return View(model);
            }

            try
            {
                var section = await _vaultSectionRepository.FindById(id);
                if (section == null) return NotFound();

                // The branch is read-only in Edit — enforce that the chosen vault still belongs to it.
                var currentBranchId = section.Vault?.BranchId;
                var newVault = await _vaultRepository.FindById(model.VaultId);

                if (newVault == null || (currentBranchId.HasValue && newVault.BranchId != currentBranchId.Value))
                {
                    ModelState.AddModelError(nameof(model.VaultId), "Selected vault must belong to the section's branch.");
                    model.BranchId = currentBranchId ?? model.BranchId;
                    await PopulateVaultList(model, model.BranchId);
                    return View(model);
                }

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

            await PopulateVaultList(model, model.BranchId);
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

                if (section.Vault != null)
                {
                    var branches = await _configServiceClient.GetAllBouBranchesAsync();
                    model.BranchId = section.Vault.BranchId;
                    model.BranchName = branches.FirstOrDefault(b => b.Id == section.Vault.BranchId)?.Name;
                }

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

        private async Task PopulateVaultList(VaultSectionViewModel model, int? branchId = null)
        {
            var vaults = await _vaultRepository.FindAll();
            var activeVaults = vaults.Where(v => v.IsActive);

            if (branchId.HasValue)
                activeVaults = activeVaults.Where(v => v.BranchId == branchId.Value);

            model.VaultList = activeVaults
                .Select(v => new SelectListItem(v.Name, v.Id.ToString()))
                .ToList();
        }

        private async Task PopulateBranchList(VaultSectionViewModel model)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BranchList = branches
                .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
                .ToList();
        }

        private async Task PopulateAllVaultsWithBranch(VaultSectionViewModel model)
        {
            var vaults = await _vaultRepository.FindAll();
            model.Vaults = vaults
                .Where(v => v.IsActive)
                .Select(v => new VaultViewModel { Id = v.Id, Name = v.Name, BranchId = v.BranchId })
                .ToList();
        }
    }
}
