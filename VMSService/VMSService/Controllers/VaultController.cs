using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Models.Vaults;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Repository.Vaults;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Static;
using VMSService.Web.Controllers;

namespace VMSService.Controllers
{
    [Authorize]
    public class VaultController : Controller
    {
        private readonly IVaultRepository _vaultRepository;
        private readonly IVaultSectionRepository _vaultSectionRepository;
        private readonly IStockService _stockService;
        private readonly IConfigurationsServiceClient _configServiceClient;

        public VaultController(
            IVaultRepository vaultRepository,
            IVaultSectionRepository vaultSectionRepository,
            IStockService stockService,
            IConfigurationsServiceClient configServiceClient)
        {
            _vaultRepository = vaultRepository;
            _vaultSectionRepository = vaultSectionRepository;
            _stockService = stockService;
            _configServiceClient = configServiceClient;
        }

        // GET: Vault
        [Breadcrumb("Vault", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index(int? branchId = null)
        {
            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            List<BranchDto> userBranches = allBouBranches
                .Where(branch => userRoles.Any(role => role.Contains(branch.Name)))
                .ToList();

            if (branchId == null)
                branchId = userBranches.FirstOrDefault()?.Id ?? 0;

            var vaults = await _vaultRepository.GetVaultDashboardAsync(branchId);
            await EnrichBagDenominations(vaults.SelectMany(v => v.Sections).SelectMany(s => s.Bags));

            ViewBag.Branches = userBranches;
            ViewBag.SelectedBranchId = branchId;

            return View(vaults);
        }

        // GET: Vault/Section/5
        [Breadcrumb("Section", FromController = typeof(VaultController), FromAction = nameof(Index))]
        public async Task<IActionResult> Section(int id)
        {
            var section = await _vaultSectionRepository.GetSectionDetailAsync(id);
            if (section == null)
            {
                TempData["Error"] = "Vault section not found.";
                return RedirectToAction(nameof(Index));
            }

            var availableBags = await _vaultSectionRepository.GetAvailableBagsAsync(section.BranchId);

            await EnrichBagDenominations(section.Bags);
            await EnrichBagDenominations(availableBags);

            section.AvailableBags = availableBags;

            return View(section);
        }

        // POST: Vault/AttachBag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachBag(AttachBagToSectionViewModel model)
        {
            try
            {
                var section = await _vaultSectionRepository.FindById(model.VaultSectionId);
                if (section != null && string.Equals(section.Name?.Trim(), VaultConstants.BagsWithBoardsSectionName, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = $"Bags cannot be manually attached to the '{VaultConstants.BagsWithBoardsSectionName}' section.";
                    return RedirectToAction(nameof(Section), new { id = model.VaultSectionId });
                }

                var bagIds = (model.BagIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();

                if (bagIds.Count == 0)
                {
                    TempData["Error"] = "Please select at least one bag to attach.";
                    return RedirectToAction(nameof(Section), new { id = model.VaultSectionId });
                }

                var attachedCount = await _vaultSectionRepository.AttachBagsAsync(bagIds, model.VaultSectionId);

                if (attachedCount == bagIds.Count)
                    TempData["Success"] = $"{attachedCount} bag{(attachedCount == 1 ? "" : "s")} attached to vault section successfully!";
                else if (attachedCount > 0)
                    TempData["Error"] = $"Attached {attachedCount} of {bagIds.Count} selected bags. Some bags could not be found.";
                else
                    TempData["Error"] = "Bags or vault section not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error attaching bags: {ex.Message}";
            }

            return RedirectToAction(nameof(Section), new { id = model.VaultSectionId });
        }

        // POST: Vault/CreateVault
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVault(CreateVaultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vault name is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var vault = new Data.Entities.Vault
                {
                    Name = model.Name.Trim(),
                    BranchId = model.BranchId,
                    IsActive = true,
                    IsDeleted = false,
                };
                await _vaultRepository.Create(vault);
                TempData["Success"] = $"Vault '{vault.Name}' created successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating vault: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Vault/DeleteVault
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVault(int id)
        {
            try
            {
                var result = await _vaultRepository.DeleteVaultAsync(id);
                if (result)
                    TempData["Success"] = "Vault deleted successfully.";
                else
                    TempData["Error"] = "Vault not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Vault/CreateSection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSection(CreateVaultSectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Section name is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var section = new Data.Entities.VaultSection
                {
                    Name = model.Name.Trim(),
                    VaultId = model.VaultId,
                    IsActive = true,
                    IsDeleted = false,
                };
                await _vaultSectionRepository.Create(section);
                TempData["Success"] = $"Section '{section.Name}' added successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating section: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Vault/DeleteSection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(int id)
        {
            try
            {
                var result = await _vaultSectionRepository.DeleteSectionAsync(id);
                if (result)
                    TempData["Success"] = "Section deleted successfully.";
                else
                    TempData["Error"] = "Section not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Vault/DetachBag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetachBag(int bagId, int vaultSectionId)
        {
            try
            {
                var result = await _vaultSectionRepository.DetachBagAsync(bagId);
                if (result)
                    TempData["Success"] = "Bag removed from vault section.";
                else
                    TempData["Error"] = "Bag not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error detaching bag: {ex.Message}";
            }

            return RedirectToAction(nameof(Section), new { id = vaultSectionId });
        }

        private async Task EnrichBagDenominations(IEnumerable<VaultBagViewModel> bags)
        {
            var allDenominations = await _stockService.GetAllDenominations();
            var denomMap = allDenominations.ToDictionary(d => d.Id);

            foreach (var bag in bags)
            {
                if (denomMap.TryGetValue(bag.DenominationId, out var denom))
                {
                    bag.DenominationName = denom.Name;
                    bag.DenominationValue = denom.Value;
                    bag.TotalAmount = denom.Value * 1000 * bag.TotalBundles;
                }
            }
        }
    }
}
