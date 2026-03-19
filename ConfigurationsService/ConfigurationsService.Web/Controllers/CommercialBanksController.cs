using AutoMapper;
using ConfigurationsService.Core.Models.CommercialBanks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConfigurationsService.Core.Repositories.CommercialBanks;
using ConfigurationsService.Data.Entities;

namespace ConfigurationsService.Web.Controllers
{
    [Authorize]
    public class CommercialBanksController : Controller
    {
        private readonly ICommercialBankRepository _commercialBankRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CommercialBanksController> _logger;

        public CommercialBanksController(ICommercialBankRepository commercialBankRepository, IMapper mapper, ILogger<CommercialBanksController> logger)
        {
            _commercialBankRepository = commercialBankRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var banks = await _commercialBankRepository.FindAll();
                var models = _mapper.Map<List<CommercialBankViewModel>>(banks);
                return View(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial banks");
                TempData["Error"] = "An error occurred while retrieving commercial banks.";
                return View(new List<CommercialBankViewModel>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var bank = await _commercialBankRepository.FindById(id);
                if (bank == null)
                {
                    TempData["Error"] = "Commercial bank not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankViewModel>(bank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank details");
                TempData["Error"] = "An error occurred while retrieving commercial bank details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            return View(new CommercialBankViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommercialBankViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var bank = _mapper.Map<CommercialBank>(model);
                //bank.CreatedDate = DateTime.UtcNow;
                //bank.CreatedBy = User.Identity?.Name ?? "System";

                await _commercialBankRepository.Create(bank);
                TempData["Success"] = "Commercial bank created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning(ex, "Duplicate commercial bank attempted");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commercial bank");
                TempData["Error"] = "An error occurred while creating the commercial bank.";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var bank = await _commercialBankRepository.FindById(id);
                if (bank == null)
                {
                    TempData["Error"] = "Commercial bank not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankViewModel>(bank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank for edit");
                TempData["Error"] = "An error occurred while retrieving the commercial bank.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommercialBankViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var bank = _mapper.Map<CommercialBank>(model);
                //bank.ModifiedDate = DateTime.UtcNow;
                //bank.ModifiedBy = User.Identity?.Name ?? "System";

                var result = await _commercialBankRepository.Update(bank);
                if (!result)
                {
                    TempData["Error"] = "Failed to update commercial bank.";
                    return View(model);
                }

                TempData["Success"] = "Commercial bank updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning(ex, "Duplicate commercial bank attempted during update");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commercial bank");
                TempData["Error"] = "An error occurred while updating the commercial bank.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var bank = await _commercialBankRepository.FindById(id);
                if (bank == null)
                {
                    TempData["Error"] = "Commercial bank not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = _mapper.Map<CommercialBankViewModel>(bank);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank for delete");
                TempData["Error"] = "An error occurred while retrieving the commercial bank.";
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
                var bank = await _commercialBankRepository.FindById(id);
                if (bank == null)
                {
                    TempData["Error"] = "Commercial bank not found.";
                    return RedirectToAction(nameof(Index));
                }

                bank.IsDeleted = true;
                //bank.ModifiedDate = DateTime.UtcNow;
                //bank.ModifiedBy = User.Identity?.Name ?? "System";

                await _commercialBankRepository.Update(bank);
                TempData["Success"] = "Commercial bank deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commercial bank");
                TempData["Error"] = "An error occurred while deleting the commercial bank.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}