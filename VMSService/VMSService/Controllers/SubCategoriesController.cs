using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Models.SubCategories;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.SubCategories;
using VMSService.Controllers;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class SubCategoriesController(
        ISubCategoryRepository _subCategoryRepository,
        ICategoryRepository _categoryRepository,
        IMapper _mapper,
        ILogger<SubCategoriesController> _logger) : Controller
    {
        [Breadcrumb("Denomination SubCategories", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: SubCategories
        public async Task<IActionResult> Index()
        {
            try
            {
                var subCategories = await _subCategoryRepository.FindAll();
                var model = _mapper.Map<List<SubCategoryViewModel>>(subCategories);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-categories");
                TempData["Error"] = "An error occurred while retrieving sub-categories. Please try again.";
                return View(new List<SubCategoryViewModel>());
            }
        }

        // GET: SubCategories/Details/5
        [Breadcrumb("Details", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var subCategory = await _subCategoryRepository.FindById(id.Value);
                bool exists = subCategory != null && !subCategory.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (subCategory == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<SubCategoryViewModel>(subCategory);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-category with id {SubCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the sub-category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: SubCategories/Create
        [Breadcrumb("Create", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var model = new SubCategoryViewModel();
            await PopulateCategoriesDropdown(model);
            return View(model);
        }

        // POST: SubCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(SubCategoryViewModel model)
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
                await PopulateCategoriesDropdown(model);
                return View(model);
            }

            try
            {
                var subCategory = _mapper.Map<SubCategory>(model);
                bool isCreated = await _subCategoryRepository.Create(subCategory);

                if (isCreated)
                {
                    TempData["Success"] = "Sub-category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create sub-category. Please try again.";
                    _logger.LogError("Error creating sub-category");
                    ModelState.AddModelError("", "Failed to create sub-category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-category");
                ModelState.AddModelError("", "Failed to create sub-category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateCategoriesDropdown(model);
            return View(model);
        }

        // GET: SubCategories/Edit/5
        [Breadcrumb("Edit", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var subCategory = await _subCategoryRepository.FindById(id.Value);
                bool exists = subCategory != null && !subCategory.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (subCategory == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<SubCategoryViewModel>(subCategory);
                await PopulateCategoriesDropdown(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-category for edit with id {SubCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the sub-category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SubCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, SubCategoryViewModel model)
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
                await PopulateCategoriesDropdown(model);
                return View(model);
            }

            try
            {
                var subCategory = await _subCategoryRepository.FindById(id);
                if (subCategory == null)
                {
                    return NotFound();
                }

                // Compare to check if changes were made
                var oldSubCategory = _mapper.Map<SubCategoryViewModel>(subCategory);
                if (oldSubCategory.Name == model.Name && oldSubCategory.CategoryId == model.CategoryId)
                {
                    TempData["Error"] = "No changes made";
                    await PopulateCategoriesDropdown(model);
                    return View(model);
                }

                subCategory.Name = model.Name;
                subCategory.CategoryId = model.CategoryId;

                bool isUpdated = await _subCategoryRepository.Update(subCategory);
                if (isUpdated)
                {
                    TempData["Success"] = "Sub-category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update sub-category. Please try again.";
                    ModelState.AddModelError("", "Failed to update sub-category. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub-category with id {SubCategoryId}", id);
                ModelState.AddModelError("", "Failed to update sub-category. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateCategoriesDropdown(model);
            return View(model);
        }

        // GET: SubCategories/Delete/5
        [Breadcrumb("Delete", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _subCategoryRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var subCategory = await _subCategoryRepository.FindById(id.Value);
                if (subCategory == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<SubCategoryViewModel>(subCategory);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub-category for deletion with id {SubCategoryId}", id);
                TempData["Error"] = "An error occurred while retrieving the sub-category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: SubCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(SubCategoriesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _subCategoryRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var subCategory = await _subCategoryRepository.FindById(id);
                if (subCategory != null)
                {
                    subCategory.IsDeleted = true;
                    bool isDeleted = await _subCategoryRepository.Update(subCategory);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Sub-category deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete sub-category. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sub-category with id {SubCategoryId}", id);
                TempData["Error"] = "An error occurred while deleting the sub-category. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to populate categories dropdown
        private async Task PopulateCategoriesDropdown(SubCategoryViewModel model)
        {
            try
            {
                var categories = await _categoryRepository.FindAll();
                model.CategoryList = categories
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == model.CategoryId
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating categories dropdown");
                model.CategoryList = new List<SelectListItem>();
            }
        }
    }
}