using AutoMapper;
using ConfigurationsService.Core.Models.CommercialBankBranches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ConfigurationsService.Core.Repositories.CommercialBankBranches;
using ConfigurationsService.Core.Repositories.CommercialBanks;
using ConfigurationsService.Core.Repositories.Districts;
using ConfigurationsService.Data.Entities;

namespace ConfigurationsService.Web.Controllers
{
    [Authorize]
    public class CommercialBankBranchesController : Controller
    {
        private readonly ICommercialBankBranchRepository _branchRepository;
        private readonly ICommercialBankRepository _bankRepository;
        private readonly IDistrictRepository _districtRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CommercialBankBranchesController> _logger;

        public CommercialBankBranchesController(
            ICommercialBankBranchRepository branchRepository,
            ICommercialBankRepository bankRepository,
            IDistrictRepository districtRepository,
            IMapper mapper,
            ILogger<CommercialBankBranchesController> logger)
        {
            _branchRepository = branchRepository;
            _bankRepository = bankRepository;
            _districtRepository = districtRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = await _branchRepository.FindAll();
                var models = _mapper.Map<List<CommercialBankBranchViewModel>>(branches);
                return View(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branches");
                TempData["Error"] = "An error occurred while retrieving commercial bank branches.";
                return View(new List<CommercialBankBranchViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Commercial bank branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankBranchViewModel>(branch);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branch details");
                TempData["Error"] = "An error occurred while retrieving commercial bank branch details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new CommercialBankBranchViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommercialBankBranchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            try
            {
                var branch = _mapper.Map<CommercialBankBranch>(model);
                //branch.CreatedDate = DateTime.UtcNow;
                //branch.CreatedBy = User.Identity?.Name ?? "System";

                await _branchRepository.Create(branch);
                TempData["Success"] = "Commercial bank branch created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning(ex, "Duplicate commercial bank branch attempted");
                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commercial bank branch");
                TempData["Error"] = "An error occurred while creating the commercial bank branch.";
                await PopulateDropdowns();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Commercial bank branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankBranchViewModel>(branch);
                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branch for edit");
                TempData["Error"] = "An error occurred while retrieving the commercial bank branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommercialBankBranchViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            try
            {
                var branch = _mapper.Map<CommercialBankBranch>(model);
                //branch.ModifiedDate = DateTime.UtcNow;
                //branch.ModifiedBy = User.Identity?.Name ?? "System";

                var result = await _branchRepository.Update(branch);
                if (!result)
                {
                    TempData["Error"] = "Failed to update commercial bank branch.";
                    await PopulateDropdowns();
                    return View(model);
                }

                TempData["Success"] = "Commercial bank branch updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning(ex, "Duplicate commercial bank branch attempted during update");
                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commercial bank branch");
                TempData["Error"] = "An error occurred while updating the commercial bank branch.";
                await PopulateDropdowns();
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Commercial bank branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankBranchViewModel>(branch);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank branch for delete");
                TempData["Error"] = "An error occurred while retrieving the commercial bank branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Commercial bank branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                branch.IsDeleted = true;
                //branch.ModifiedDate = DateTime.UtcNow;
                //branch.ModifiedBy = User.Identity?.Name ?? "System";

                await _branchRepository.Update(branch);
                TempData["Success"] = "Commercial bank branch deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commercial bank branch");
                TempData["Error"] = "An error occurred while deleting the commercial bank branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateDropdowns()
        {
            var banks = await _bankRepository.FindAll();
            var districts = await _districtRepository.FindAll();

            ViewData["CommercialBankId"] = new SelectList(banks, "Id", "Name");
            ViewData["DistrictId"] = new SelectList(districts, "Id", "Name");
        }
    }
}