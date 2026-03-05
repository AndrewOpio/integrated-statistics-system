using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Categories;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CategoriesController(
        ICategoryRepository _categoryRepository,
        IMapper _mapper,
        ILogger<CategoriesController> _logger) : Controller
    {
        [Breadcrumb("Currency Categories", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: Categories
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = (await _categoryRepository.FindAll());
                var model = _mapper.Map<List<CategoryViewModel>>(categories);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                TempData["Error"] = "An error occurred while retrieving categories. Please try again.";
                return View(new List<CategoryViewModel>());
            }
        }

        // GET: Categories/Details/5
        [Breadcrumb("Details", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _categoryRepository.FindById(id.Value);
                bool exists = category != null && !category.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (category == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CategoryViewModel>(category);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with id {CategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Categories/Create
        [Breadcrumb("Create", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CategoryViewModel model)
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
                var category = _mapper.Map<Category>(model);
                bool isCreated = await _categoryRepository.Create(category);

                if (isCreated)
                {
                    TempData["Success"] = "Category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create category. Please try again.";
                    _logger.LogError("Error creating category");
                    ModelState.AddModelError("", "Failed to create category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                ModelState.AddModelError("", "Failed to create category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: Categories/Edit/5
        [Breadcrumb("Edit", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _categoryRepository.FindById(id.Value);
                bool exists = category != null && !category.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (category == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CategoryViewModel>(category);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category for edit with id {CategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
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
                var category = await _categoryRepository.FindById(id);
                if (category == null)
                {
                    return NotFound();
                }

                //Compare to check if changes were made
                var oldCategory = _mapper.Map<CategoryViewModel>(category);
                if (oldCategory.Name == model.Name && oldCategory.Classification == model.Classification)
                {
                    TempData["Error"] = "No changes Made";
                    return View(model);
                }

                category.Name = model.Name;
                category.Classification = model.Classification;

                bool isUpdated = await _categoryRepository.Update(category);
                if (isUpdated)
                {
                    TempData["Success"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update category. Please try again.";
                    ModelState.AddModelError("", "Failed to update category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with id {CategoryId}", id);
                ModelState.AddModelError("", "Failed to update category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: Categories/Delete/5
        [Breadcrumb("Delete", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _categoryRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var category = await _categoryRepository.FindById(id.Value);
                if (category == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<CategoryViewModel>(category);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category for deletion with id {CategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _categoryRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var category = await _categoryRepository.FindById(id);
                if (category != null)
                {
                    category.IsDeleted = true;
                    bool isDeleted = await _categoryRepository.Update(category);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Category deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete category. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with id {CategoryId}", id);
                TempData["Error"] = "An error occurred while deleting the category. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}