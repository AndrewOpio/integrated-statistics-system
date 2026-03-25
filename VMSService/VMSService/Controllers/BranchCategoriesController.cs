using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.BranchCategories;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BranchCategoriesController(
        IBranchCategoryRepository _branchCategoryRepository,
        IBranchRepository _branchRepository,
        ICategoryRepository _categoryRepository,
        IMapper _mapper,
        ILogger<BranchCategoriesController> _logger) : Controller
    {
        [Breadcrumb("Vault Stores", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: BranchCategories
        public async Task<IActionResult> Index()
        {
            try
            {
                var branchCategories = (await _branchCategoryRepository.FindAll());
                var model = _mapper.Map<List<BranchCategoryViewModel>>(branchCategories);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch categories");
                TempData["Error"] = "An error occurred while retrieving branch categories. Please try again.";
                return View(new List<BranchCategoryViewModel>());
            }
        }

        // GET: BranchCategories/Details/5
        [Breadcrumb("Details", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var branchCategory = await _branchCategoryRepository.FindById(id.Value);
                bool exists = branchCategory != null && !branchCategory.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (branchCategory == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<BranchCategoryViewModel>(branchCategory);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch category with id {BranchCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: BranchCategories/Create
        [Breadcrumb("Create", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var branches = await _branchRepository.FindAll();
                var categories = await _categoryRepository.FindAll();
                ViewBag.Branches = branches.ToList();
                ViewBag.Categories = categories.Where(c => !c.IsDeleted).ToList();
                return View(new BranchCategoryViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches and categories for create");
                TempData["Error"] = "An error occurred while loading branches and categories. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BranchCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(BranchCategoryViewModel model)
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

                var branches = await _branchRepository.FindAll();
                var categories = await _categoryRepository.FindAll();
                ViewBag.Branches = branches.ToList();
                ViewBag.Categories = categories.Where(c => !c.IsDeleted).ToList();
                return View(model);
            }

            try
            {
                var branchCategory = _mapper.Map<BranchCategory>(model);
                bool isCreated = await _branchCategoryRepository.Create(branchCategory);

                if (isCreated)
                {
                    TempData["Success"] = "Branch category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create branch category. Please try again.";
                    _logger.LogError("Error creating branch category");
                    ModelState.AddModelError("", "Failed to create branch category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch category");
                ModelState.AddModelError("", "Failed to create branch category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var branchList = await _branchRepository.FindAll();
            var catList = await _categoryRepository.FindAll();
            ViewBag.Branches = branchList.ToList();
            ViewBag.Categories = catList.Where(c => !c.IsDeleted).ToList();
            return View(model);
        }

        // GET: BranchCategories/Edit/5
        [Breadcrumb("Edit", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var branchCategory = await _branchCategoryRepository.FindById(id.Value);
                bool exists = branchCategory != null && !branchCategory.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (branchCategory == null)
                {
                    return NotFound();
                }

                var branches = await _branchRepository.FindAll();
                var categories = await _categoryRepository.FindAll();
                ViewBag.Branches = branches.ToList();
                ViewBag.Categories = categories.Where(c => !c.IsDeleted).ToList();

                var model = _mapper.Map<BranchCategoryViewModel>(branchCategory);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch category for edit with id {BranchCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BranchCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, BranchCategoryViewModel model)
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

                var branches = await _branchRepository.FindAll();
                var categories = await _categoryRepository.FindAll();
                ViewBag.Branches = branches.ToList();
                ViewBag.Categories = categories.Where(c => !c.IsDeleted).ToList();
                return View(model);
            }

            try
            {
                var branchCategory = await _branchCategoryRepository.FindById(id);
                if (branchCategory == null)
                {
                    return NotFound();
                }

                var oldBranchCategory = _mapper.Map<BranchCategoryViewModel>(branchCategory);
                if (oldBranchCategory.BranchId == model.BranchId && oldBranchCategory.CategoryId == model.CategoryId)
                {
                    var branches = await _branchRepository.FindAll();
                    var categories = await _categoryRepository.FindAll();
                    ViewBag.Branches = branches.ToList();
                    ViewBag.Categories = categories.Where(c => !c.IsDeleted).ToList();
                    TempData["Error"] = "No changes Made";
                    return View(model);
                }

                branchCategory.BranchId = model.BranchId;
                branchCategory.CategoryId = model.CategoryId;

                bool isUpdated = await _branchCategoryRepository.Update(branchCategory);
                if (isUpdated)
                {
                    TempData["Success"] = "Branch category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update branch category. Please try again.";
                    ModelState.AddModelError("", "Failed to update branch category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch category with id {BranchCategoryId}", id);
                ModelState.AddModelError("", "Failed to update branch category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            var branchList = await _branchRepository.FindAll();
            var catList = await _categoryRepository.FindAll();
            ViewBag.Branches = branchList.ToList();
            ViewBag.Categories = catList.Where(c => !c.IsDeleted).ToList();
            return View(model);
        }

        // GET: BranchCategories/Delete/5
        [Breadcrumb("Delete", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _branchCategoryRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var branchCategory = await _branchCategoryRepository.FindById(id.Value);
                if (branchCategory == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<BranchCategoryViewModel>(branchCategory);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch category for deletion with id {BranchCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BranchCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(BranchCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _branchCategoryRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var branchCategory = await _branchCategoryRepository.FindById(id);
                if (branchCategory != null)
                {
                    branchCategory.IsDeleted = true;
                    bool isDeleted = await _branchCategoryRepository.Update(branchCategory);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Branch category deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete branch category. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch category with id {BranchCategoryId}", id);
                TempData["Error"] = "An error occurred while deleting the branch category. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}