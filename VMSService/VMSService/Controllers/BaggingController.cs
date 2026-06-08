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

        public BaggingController(IBagRepository bagRepository, IStockService stockService)
        {
            _bagRepository = bagRepository;
            _stockService = stockService;
        }

        // GET: Bagging
        [Breadcrumb("Bagging", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            var bags = await _bagRepository.GetAllBagsAsync();
            
            // Enrich with denomination names
            foreach (var bag in bags)
            {
                var denomination = await _stockService.GetDenominationById(bag.DenominationId);
                bag.DenominationName = denomination?.Name;
                bag.DenominationValue = denomination?.Value ?? 0;
            }

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

            foreach (var item in bag.Items)
            {
                var itemDenom = await _stockService.GetDenominationById(item.DenominationId);
                item.DenominationName = itemDenom?.Name;

                var subCat = await _stockService.GetSubCategoryById(item.SubCategoryId);
                item.SubCategoryName = subCat?.Name;
            }

            return View(bag);
        }

        // GET: Bagging/Create
        [Breadcrumb("Create", FromController = typeof(BaggingController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            ViewBag.Denominations = await _stockService.GetAllDenominations();
            return View();
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
                return RedirectToAction(nameof(AddItems), new { id = bag!.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating bag: {ex.Message}";
                ViewBag.Denominations = await _stockService.GetAllDenominations();
                return View(model);
            }
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
    }
}