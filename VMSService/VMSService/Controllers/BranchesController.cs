using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.Branches;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BranchesController(
        IBranchRepository _branchRepository,
        IMapper _mapper,
        ILogger<BranchesController> _logger) : Controller
    {
        [Breadcrumb("Branches", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: Branches
        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = (await _branchRepository.FindAll());
                var model = _mapper.Map<List<BranchViewModel>>(branches);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches");
                TempData["Error"] = "An error occurred while retrieving branches. Please try again.";
                return View(new List<BranchViewModel>());
            }
        }

        // GET: Branches/Details/5
        [Breadcrumb("Details", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var branch = await _branchRepository.FindById(id.Value);
                bool exists = branch != null && !branch.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (branch == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<BranchViewModel>(branch);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch with id {BranchId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Branches/Create
        [Breadcrumb("Create", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new BranchViewModel());
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(BranchViewModel model)
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
                var branch = _mapper.Map<Branch>(model);
                bool isCreated = await _branchRepository.Create(branch);

                if (isCreated)
                {
                    TempData["Success"] = "Branch created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create branch. Please try again.";
                    _logger.LogError("Error creating branch");
                    ModelState.AddModelError("", "Failed to create branch. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                ModelState.AddModelError("", "Failed to create branch. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: Branches/Edit/5
        [Breadcrumb("Edit", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var branch = await _branchRepository.FindById(id.Value);
                bool exists = branch != null && !branch.IsDeleted;
                if (!exists)
                {
                    return NotFound();
                }

                if (branch == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<BranchViewModel>(branch);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch for edit with id {BranchId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, BranchViewModel model)
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
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    return NotFound();
                }

                //Compare to check if changes were made
                var oldBranch = _mapper.Map<BranchViewModel>(branch);
                if (oldBranch.Name == model.Name)
                {
                    TempData["Error"] = "No changes Made";
                    return View(model);
                }

                branch.Name = model.Name;

                bool isUpdated = await _branchRepository.Update(branch);
                if (isUpdated)
                {
                    TempData["Success"] = "Branch updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update branch. Please try again.";
                    ModelState.AddModelError("", "Failed to update branch. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch with id {BranchId}", id);
                ModelState.AddModelError("", "Failed to update branch. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            return View(model);
        }

        // GET: Branches/Delete/5
        [Breadcrumb("Delete", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _branchRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var branch = await _branchRepository.FindById(id.Value);
                if (branch == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<BranchViewModel>(branch);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch for deletion with id {BranchId}", id);
                TempData["Error"] = "An error occurred while retrieving the branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(BranchesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _branchRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var branch = await _branchRepository.FindById(id);
                if (branch != null)
                {
                    branch.IsDeleted = true;
                    bool isDeleted = await _branchRepository.Update(branch);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Branch deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete branch. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch with id {BranchId}", id);
                TempData["Error"] = "An error occurred while deleting the branch. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}