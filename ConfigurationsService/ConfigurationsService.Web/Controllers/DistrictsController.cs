using AutoMapper;
using ConfigurationsService.Core.Models.Districts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationsService.Core.Repositories.Districts;
using ConfigurationsService.Data.Entities;

namespace ConfigurationsService.Web.Controllers
{
    [Authorize]
    public class DistrictsController : Controller
    {
        private readonly IDistrictRepository _districtRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DistrictsController> _logger;

        public DistrictsController(IDistrictRepository districtRepository, IMapper mapper, ILogger<DistrictsController> logger)
        {
            _districtRepository = districtRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var districts = await _districtRepository.FindAll();
                var model = _mapper.Map<List<DistrictViewModel>>(districts);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving districts");
                TempData["Error"] = "An error occurred while retrieving districts.";
                return View(new List<DistrictViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var district = await _districtRepository.FindById(id);
                if (district == null)
                {
                    TempData["Error"] = "District not found.";
                    return RedirectToAction(nameof(Index));
                }

                var DistrictViewModel = _mapper.Map<DistrictViewModel>(district);
                return View(DistrictViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving district details");
                TempData["Error"] = "An error occurred while retrieving district details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            return View(new DistrictViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistrictViewModel DistrictViewModel)
        {
            if (!ModelState.IsValid)
                return View(DistrictViewModel);

            try
            {
                var district = _mapper.Map<District>(DistrictViewModel);
                //district.CreatedDate = DateTime.UtcNow;
                //district.CreatedBy = User.Identity?.Name ?? "System";

                await _districtRepository.Create(district);
                TempData["Success"] = "District created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                _logger.LogWarning(ex, "Duplicate district name attempted");
                return View(DistrictViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating district");
                TempData["Error"] = "An error occurred while creating the district.";
                return View(DistrictViewModel);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var district = await _districtRepository.FindById(id);
                if (district == null)
                {
                    TempData["Error"] = "District not found.";
                    return RedirectToAction(nameof(Index));
                }

                var DistrictViewModel = _mapper.Map<DistrictViewModel>(district);
                return View(DistrictViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving district for edit");
                TempData["Error"] = "An error occurred while retrieving the district.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DistrictViewModel DistrictViewModel)
        {
            if (id != DistrictViewModel.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(DistrictViewModel);

            try
            {
                var district = _mapper.Map<District>(DistrictViewModel);
                //district.ModifiedDate = DateTime.UtcNow;
                //district.ModifiedBy = User.Identity?.Name ?? "System";

                var result = await _districtRepository.Update(district);
                if (!result)
                {
                    TempData["Error"] = "Failed to update district.";
                    return View(DistrictViewModel);
                }

                TempData["Success"] = "District updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                _logger.LogWarning(ex, "Duplicate district name attempted during update");
                return View(DistrictViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating district");
                TempData["Error"] = "An error occurred while updating the district.";
                return View(DistrictViewModel);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var district = await _districtRepository.FindById(id);
                if (district == null)
                {
                    TempData["Error"] = "District not found.";
                    return RedirectToAction(nameof(Index));
                }

                var DistrictViewModel = _mapper.Map<DistrictViewModel>(district);
                return View(DistrictViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving district for delete");
                TempData["Error"] = "An error occurred while retrieving the district.";
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
                var district = await _districtRepository.FindById(id);
                if (district == null)
                {
                    TempData["Error"] = "District not found.";
                    return RedirectToAction(nameof(Index));
                }

                district.IsDeleted = true;
                //district.ModifiedDate = DateTime.UtcNow;
                //district.ModifiedBy = User.Identity?.Name ?? "System";

                await _districtRepository.Update(district);
                TempData["Success"] = "District deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting district");
                TempData["Error"] = "An error occurred while deleting the district.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}