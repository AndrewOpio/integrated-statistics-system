using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Models.Bags;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Static;
using VMSService.Web.Controllers;

namespace VMSService.Controllers
{
    [Authorize]
    public class BaggingController : Controller
    {
        private readonly IBagRepository _bagRepository;
        private readonly IStockService _stockService;
        private readonly IConfigurationsServiceClient _configServiceClient;

        public BaggingController(IBagRepository bagRepository, IStockService stockService, IConfigurationsServiceClient configServiceClient)
        {
            _bagRepository = bagRepository;
            _stockService = stockService;
            _configServiceClient = configServiceClient;
        }

        // GET: Bagging
        [Breadcrumb("Bagging", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index(int? branchId = null)
        {
            var allBouBranches = await _configServiceClient.GetAllBouBranchesAsync();

            // Get user roles
            var userRoles = User.FindAll("appRole")
                .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1])
                .ToList();

            // Filter branches based on user roles
            // Filter branches based on role names matching branch names
            List<BranchDto> userBranches = allBouBranches
                .Where(branch => userRoles.Any(role => role.Contains(branch.Name)))
                .ToList();

            if (branchId == null)
            {
                branchId = userBranches.FirstOrDefault()?.Id ?? 0;
            }

            var bags = await _bagRepository.GetAllBagsAsync(branchId);

            // Enrich with denomination names
            var denominations = await _stockService.GetAllDenominations();
            var denomMap = denominations.ToDictionary(d => d.Id);
            foreach (var bag in bags)
            {
                if (denomMap.TryGetValue(bag.DenominationId, out var denom))
                {
                    bag.DenominationName = denom.Name;
                    bag.DenominationValue = denom.Value;
                }
                bag.TotalAmount = bag.DenominationValue * 1000 * bag.Items.Sum(bd => bd.NumberOfBundlesOrBoxes);
            }

            ViewBag.Branches = userBranches;
            ViewBag.SelectedBranchId = branchId;

            return View(bags);
        }

        // GET: Bagging/Details/5
        [Breadcrumb("Details", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var bag = await _bagRepository.GetBagByIdAsync(id);
            if (bag == null)
            {
                TempData["Error"] = "Bag not found.";
                return RedirectToAction(nameof(Index));
            }

            // Enrich with microservice data
            var denomination = await _stockService.GetDenominationById(bag.DenominationId);
            bag.DenominationName = denomination?.Name;
            bag.DenominationValue = denomination?.Value ?? 0;
            bag.TotalAmount = bag.DenominationValue * 1000 * bag.TotalBundles;

            foreach (var item in bag.Items)
            {
                var itemDenom = await _stockService.GetDenominationById(item.DenominationId);
                item.DenominationName = itemDenom?.Name;
                item.DenominationValue = itemDenom?.Value ?? 0;
                item.Amount = item.DenominationValue * 1000 * item.NumberOfBundlesOrBoxes;

                var subCat = await _stockService.GetSubCategoryById(item.SubCategoryId);
                item.SubCategoryName = subCat?.Name;

                var commercialBank = await _configServiceClient.GetCbIdAsync(item.CbId);
                item.CommercialBankName = commercialBank.Name;
            }

            foreach (var nested in bag.NestedBags)
            {
                nested.TotalAmount = bag.DenominationValue * 1000 * nested.TotalBundles;
            }

            return View(bag);
        }

        // GET: Bagging/Create
        [Breadcrumb("Create", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(int branchId = 0)
        {
            var denominations = await _stockService.GetAllDenominations();
            var selectedDenomination = denominations.FirstOrDefault();
            int bagSequence = await _bagRepository.BagSequence(DateTime.UtcNow, selectedDenomination?.Id ?? 0);

            // Generate denomination short form
            string denomShortForm = GetDenominationShortForm(selectedDenomination?.Value ?? 0);

            ViewBag.Denominations = denominations;

            CreateBagViewModel model = new CreateBagViewModel
            {
                ReferenceNumber = $"BAG-{DateTime.UtcNow:ddMMyy}-{denomShortForm}-{bagSequence}",
                DenominationId = selectedDenomination?.Id ?? 0,
                BranchId = branchId
            };

            return View(model);
        }

        // POST: Bagging/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CreateBagViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Denominations = await _stockService.GetAllDenominations();
                return View(model);
            }

            try
            {
                var bag = await _bagRepository.CreateBagAsync(model);
                TempData["Success"] = $"Bag '{model.ReferenceNumber}' created successfully!";
                return RedirectToAction(nameof(Details), new { id = bag!.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating bag: {ex.Message}";
                ViewBag.Denominations = await _stockService.GetAllDenominations();
                return View(model);
            }
        }

        // GET: Bagging/GenerateReferenceNumber
        [HttpGet]
        public async Task<IActionResult> GenerateReferenceNumber(int denominationId)
        {
            var denomination = await _stockService.GetDenominationById(denominationId);
            if (denomination == null)
                return NotFound(new { error = "Denomination not found." });

            int bagSequence = await _bagRepository.BagSequence(DateTime.UtcNow, denominationId);
            string denomShortForm = GetDenominationShortForm(denomination.Value);
            string referenceNumber = $"BAG-{DateTime.UtcNow:ddMMyy}-{denomShortForm}-{bagSequence}";

            return Json(new { referenceNumber });
        }

        // GET: Bagging/AddItems/5
        [Breadcrumb("Add Items", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> AddItems(int id)
        {
            var bag = await _bagRepository.GetBagByIdAsync(id);
            if (bag == null)
            {
                TempData["Error"] = "Bag not found.";
                return RedirectToAction(nameof(Index));
            }

            var availableItems = await _bagRepository.GetAvailableItemsByDenominationAsync(bag.DenominationId);
            
            // Enrich with microservice data
            foreach (var item in availableItems)
            {
                var denom = await _stockService.GetDenominationById(item.DenominationId);
                item.DenominationName = denom?.Name;

                var subCat = await _stockService.GetSubCategoryById(item.SubCategoryId);
                item.SubCategoryName = subCat?.Name;
            }

            var bagDenom = await _stockService.GetDenominationById(bag.DenominationId);
            bag.DenominationName = bagDenom.Name;

            ViewBag.Bag = bag;
            ViewBag.AvailableItems = availableItems;
            
            return View();
        }

        // POST: Bagging/AddItems
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Add Items", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> AddItems(AddItemsToBagViewModel model)
        {
            if (!ModelState.IsValid || !model.DepositRequestDenominationIds.Any())
            {
                TempData["Error"] = "Please select at least one item to add.";
                return RedirectToAction(nameof(AddItems), new { id = model.BagId });
            }

            try
            {
                await _bagRepository.AddItemsToBagAsync(model);
                TempData["Success"] = $"{model.DepositRequestDenominationIds.Count} item(s) added to bag successfully!";
                return RedirectToAction(nameof(Details), new { id = model.BagId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error adding items: {ex.Message}";
                return RedirectToAction(nameof(AddItems), new { id = model.BagId });
            }
        }

        // POST: Bagging/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Remove Items", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> RemoveItem(int bagId, Guid itemId)
        {
            try
            {
                var result = await _bagRepository.RemoveItemFromBagAsync(bagId, itemId);
                if (result)
                {
                    TempData["Success"] = "Item removed from bag successfully!";
                }
                else
                {
                    TempData["Error"] = "Item not found in bag.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error removing item: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = bagId });
        }

        // POST: Bagging/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Remove Items", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _bagRepository.DeleteBagAsync(id);
                if (result)
                {
                    TempData["Success"] = "Bag deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Bag not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting bag: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Bagging/AddBag/5
        [Breadcrumb("Add Bag", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> AddBag(int id)
        {
            var parentBag = await _bagRepository.GetBagByIdAsync(id);
            if (parentBag == null)
            {
                TempData["Error"] = "Bag not found.";
                return RedirectToAction(nameof(Index));
            }

            var denomination = await _stockService.GetDenominationById(parentBag.DenominationId);
            parentBag.DenominationName = denomination?.Name;
            parentBag.DenominationValue = denomination?.Value ?? 0;
            parentBag.TotalAmount = parentBag.DenominationValue * 1000 * parentBag.TotalBundles;

            var availableBags = await _bagRepository.GetAvailableBagsForMergingAsync(id);
            foreach (var bag in availableBags)
            {
                bag.TotalAmount = parentBag.DenominationValue * 1000 * bag.TotalBundles;
            }

            ViewBag.ParentBag = parentBag;
            ViewBag.AvailableBags = availableBags;

            return View(new AddBagToBagViewModel { ParentBagId = id });
        }

        // POST: Bagging/AddBag
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Add Bag", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> AddBag(AddBagToBagViewModel model)
        {
            try
            {
                await _bagRepository.AddBagToBagAsync(model);
                TempData["Success"] = "Bag added successfully!";
                return RedirectToAction(nameof(Details), new { id = model.ParentBagId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error adding bag: {ex.Message}";
                return RedirectToAction(nameof(AddBag), new { id = model.ParentBagId });
            }
        }

        // POST: Bagging/RemoveBag
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBag(int baggedBagId, int parentBagId)
        {
            try
            {
                var result = await _bagRepository.RemoveBagFromBagAsync(baggedBagId);
                if (result)
                    TempData["Success"] = "Bag removed successfully!";
                else
                    TempData["Error"] = "Nested bag record not found.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error removing bag: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = parentBagId });
        }

        private string GetDenominationShortForm(decimal denominationValue)
        {
            if (denominationValue >= 1000)
            {
                // Convert to thousands and append 'K'
                int thousands = (int)(denominationValue / 1000);
                return $"{thousands}K";
            }
            else
            {
                // For values less than 1000, return as is
                return denominationValue.ToString("0");
            }
        }
    }
}