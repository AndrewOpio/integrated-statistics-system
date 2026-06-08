using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.MinDepositStandards;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.MinDepositStandards;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class MinDepositStandardsController(
        IMinDepositStandardRepository _minDepositStandardRepository,
        IDenominationRepository _denominationRepository,
        IBranchRepository _branchRepository,
        IMapper _mapper,
        ILogger<MinDepositStandardsController> _logger) : Controller
    {
        private async Task<List<SelectListItem>> GetDenominationSelectList()
        {
            var denominations = await _denominationRepository.FindAll();
            return denominations
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.Value)
                .Select(d => new SelectListItem { Text = d.Name, Value = d.Id.ToString() })
                .ToList();
        }

        private async Task<List<SelectListItem>> GetBranchSelectList()
        {
            var branches = await _branchRepository.FindAll();
            return branches
                .Select(b => new SelectListItem { Text = b.Name, Value = b.Id.ToString() })
                .ToList();
        }

        private async Task PopulateDropdowns(MinDepositStandardViewModel model)
        {
            model.DenominationList = await GetDenominationSelectList();
            model.BranchList = await GetBranchSelectList();
            
            // Get denomination data for JavaScript
            var denominations = await _denominationRepository.FindAll();
            var denominationData = denominations
                .Where(d => !d.IsDeleted)
                .Select(d => new
                {
                    id = d.Id,
                    value = d.Value,
                    numberOfPiecesInABundle = d.NumberOfPiecesInABundle
                })
                .ToList();
            ViewBag.DenominationData = JsonConvert.SerializeObject(denominationData);
        }

        private async Task PopulateDisplayNames(MinDepositStandardViewModel model)
        {
            if (model.DenominationId > 0)
            {
                var denomination = await _denominationRepository.FindById(model.DenominationId);
                model.DenominationName = denomination?.Name;
            }
            if (model.BranchId > 0)
            {
                var branch = await _branchRepository.FindById(model.BranchId);
                model.BranchName = branch?.Name;
            }
        }

        private async Task<bool> IsDuplicateDenominationBranchCombo(int denominationId, int branchId, int? excludeId = null)
        {
            var allStandards = await _minDepositStandardRepository.FindAll();
            return allStandards.Any(s => s.DenominationId == denominationId && 
                                        s.BranchId == branchId &&
                                        s.Id != (excludeId ?? 0) && 
                                        !s.IsDeleted);
        }

        [Breadcrumb("Min Deposit Standards", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: MinDepositStandards
        public async Task<IActionResult> Index()
        {
            try
            {
                var standards = (await _minDepositStandardRepository.FindAll()).Where(x => x.IsDeleted == false);
                var model = _mapper.Map<List<MinDepositStandardViewModel>>(standards);

                foreach (var item in model)
                {
                    await PopulateDisplayNames(item);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving min deposit standards");
                TempData["Error"] = "An error occurred while retrieving min deposit standards. Please try again.";
                return View(new List<MinDepositStandardViewModel>());
            }
        }

        // GET: MinDepositStandards/Details/5
        [Breadcrumb("Details", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _minDepositStandardRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var standard = await _minDepositStandardRepository.FindById(id.Value);
                if (standard == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<MinDepositStandardViewModel>(standard);
                await PopulateDisplayNames(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving min deposit standard with id {MinDepositStandardId}", id);
                TempData["Error"] = "An error occurred while retrieving the min deposit standard. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: MinDepositStandards/Create
        [Breadcrumb("Create", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new MinDepositStandardViewModel();
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data for create");
                TempData["Error"] = "An error occurred while loading data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MinDepositStandards/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(MinDepositStandardViewModel model)
        {
            // Check for duplicate denomination and branch combination
            if (await IsDuplicateDenominationBranchCombo(model.DenominationId, model.BranchId))
            {
                var denomination = await _denominationRepository.FindById(model.DenominationId);
                var branch = await _branchRepository.FindById(model.BranchId);
                ModelState.AddModelError("", 
                    $"A min deposit standard already exists for '{denomination?.Name}' at '{branch?.Name}'. Please select a different combination.");
                TempData["Error"] = $"A min deposit standard already exists for '{denomination?.Name}' at '{branch?.Name}'.";
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }

                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var standard = _mapper.Map<MinDepositStandard>(model);
                bool isCreated = await _minDepositStandardRepository.Create(standard);

                if (isCreated)
                {
                    TempData["Success"] = "Min deposit standard created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create min deposit standard. Please try again.";
                    _logger.LogError("Error creating min deposit standard");
                    ModelState.AddModelError("", "Failed to create min deposit standard. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating min deposit standard");
                ModelState.AddModelError("", "Failed to create min deposit standard. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: MinDepositStandards/Edit/5
        [Breadcrumb("Edit", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _minDepositStandardRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var standard = await _minDepositStandardRepository.FindById(id.Value);
                if (standard == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<MinDepositStandardViewModel>(standard);
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving min deposit standard for edit with id {MinDepositStandardId}", id);
                TempData["Error"] = "An error occurred while retrieving the min deposit standard. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MinDepositStandards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, MinDepositStandardViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Check for duplicate denomination and branch combination (excluding current record)
            if (await IsDuplicateDenominationBranchCombo(model.DenominationId, model.BranchId, id))
            {
                var denomination = await _denominationRepository.FindById(model.DenominationId);
                var branch = await _branchRepository.FindById(model.BranchId);
                ModelState.AddModelError("", 
                    $"A min deposit standard already exists for '{denomination?.Name}' at '{branch?.Name}'. Please select a different combination.");
                TempData["Error"] = $"A min deposit standard already exists for '{denomination?.Name}' at '{branch?.Name}'.";
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }

                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var standard = await _minDepositStandardRepository.FindById(id);
                if (standard == null)
                {
                    return NotFound();
                }

                standard.DenominationId = model.DenominationId;
                standard.BranchId = model.BranchId;
                standard.MinimumBundles = model.MinimumBundles;
                standard.Value = model.Value;

                bool isUpdated = await _minDepositStandardRepository.Update(standard);
                if (isUpdated)
                {
                    TempData["Success"] = "Min deposit standard updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update min deposit standard. Please try again.";
                    ModelState.AddModelError("", "Failed to update min deposit standard. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating min deposit standard with id {MinDepositStandardId}", id);
                ModelState.AddModelError("", "Failed to update min deposit standard. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: MinDepositStandards/Delete/5
        [Breadcrumb("Delete", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _minDepositStandardRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var standard = await _minDepositStandardRepository.FindById(id.Value);
                if (standard == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<MinDepositStandardViewModel>(standard);
                await PopulateDisplayNames(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving min deposit standard for deletion with id {MinDepositStandardId}", id);
                TempData["Error"] = "An error occurred while retrieving the min deposit standard. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MinDepositStandards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(MinDepositStandardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _minDepositStandardRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var standard = await _minDepositStandardRepository.FindById(id);
                if (standard != null)
                {
                    standard.IsDeleted = true;
                    bool isDeleted = await _minDepositStandardRepository.Update(standard);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Min deposit standard deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete min deposit standard. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting min deposit standard with id {MinDepositStandardId}", id);
                TempData["Error"] = "An error occurred while deleting the min deposit standard. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}