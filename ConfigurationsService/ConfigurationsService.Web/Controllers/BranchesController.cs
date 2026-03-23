using AutoMapper;
using ConfigurationsService.Core.Models.Branches;
using ConfigurationsService.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Repositories.Branches;

namespace ConfigurationsService.Web.Controllers
{
    //[Authorize]
    public class BranchesController : Controller
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(IBranchRepository branchRepository, IMapper mapper, ILogger<BranchesController> logger)
        {
            _branchRepository = branchRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Branches/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = await _branchRepository.FindAll();
                var branchDtos = _mapper.Map<List<BranchDto>>(branches);
                return View(branchDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches");
                TempData["Error"] = "An error occurred while retrieving branches.";
                return View(new List<BranchDto>());
            }
        }

        // GET: Branches/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var branchDto = _mapper.Map<BranchDto>(branch);
                return View(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch details");
                TempData["Error"] = "An error occurred while retrieving branch details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Branches/Create
        public IActionResult Create()
        {
            return View(new BranchDto());
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchDto branchDto)
        {
            if (!ModelState.IsValid)
                return View(branchDto);

            try
            {
                var branch = _mapper.Map<Branch>(branchDto);
                branch.CreatedDate = DateTime.UtcNow;
                branch.CreatedBy = User.Identity?.Name ?? "System";

                await _branchRepository.Create(branch);
                TempData["Success"] = "Branch created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                _logger.LogWarning(ex, "Duplicate branch name attempted");
                return View(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                TempData["Error"] = "An error occurred while creating the branch.";
                return View(branchDto);
            }
        }

        // GET: Branches/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var branchDto = _mapper.Map<BranchDto>(branch);
                return View(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch for edit");
                TempData["Error"] = "An error occurred while retrieving the branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BranchDto branchDto)
        {
            if (id != branchDto.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(branchDto);

            try
            {
                var branch = _mapper.Map<Branch>(branchDto);
                branch.ModifiedDate = DateTime.UtcNow;
                branch.ModifiedBy = User.Identity?.Name ?? "System";

                var result = await _branchRepository.Update(branch);
                if (!result)
                {
                    TempData["Error"] = "Failed to update branch.";
                    return View(branchDto);
                }

                TempData["Success"] = "Branch updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                _logger.LogWarning(ex, "Duplicate branch name attempted during update");
                return View(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch");
                TempData["Error"] = "An error occurred while updating the branch.";
                return View(branchDto);
            }
        }

        // GET: Branches/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var branch = await _branchRepository.FindById(id);
                if (branch == null)
                {
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                var branchDto = _mapper.Map<BranchDto>(branch);
                return View(branchDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch for delete");
                TempData["Error"] = "An error occurred while retrieving the branch.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Delete/5
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
                    TempData["Error"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }

                branch.IsDeleted = true;
                branch.ModifiedDate = DateTime.UtcNow;
                branch.ModifiedBy = User.Identity?.Name ?? "System";

                await _branchRepository.Update(branch);
                TempData["Success"] = "Branch deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch");
                TempData["Error"] = "An error occurred while deleting the branch.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}