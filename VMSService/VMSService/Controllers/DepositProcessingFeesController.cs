using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.DepositProcessingFees;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositProcessingFees;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class DepositProcessingFeesController(
        IDepositProcessingFeeRepository _depositProcessingFeeRepository,
        IDenominationTypeRepository _denominationTypeRepository,
        IMapper _mapper,
        ILogger<DepositProcessingFeesController> _logger) : Controller
    {
        private async Task<List<SelectListItem>> GetDenominationTypeSelectList()
        {
            var denominationTypes = await _denominationTypeRepository.FindAll();
            return denominationTypes
                .Where(dt => !dt.IsDeleted)
                .Select(dt => new SelectListItem { Text = dt.Name, Value = dt.Id.ToString() })
                .ToList();
        }

        private async Task PopulateDropdowns(DepositProcessingFeeViewModel model)
        {
            model.DenominationTypeList = await GetDenominationTypeSelectList();
        }

        private async Task PopulateDisplayNames(DepositProcessingFeeViewModel model)
        {
            if (model.DenominationTypeId > 0)
            {
                var denominationType = await _denominationTypeRepository.FindById(model.DenominationTypeId);
                model.DenominationTypeName = denominationType?.Name;
            }
        }

        [Breadcrumb("Deposit Processing Fees", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: DepositProcessingFees
        public async Task<IActionResult> Index()
        {
            try
            {
                var fees = (await _depositProcessingFeeRepository.FindAll()).Where(f => f.IsDeleted == false);
                var model = _mapper.Map<List<DepositProcessingFeeViewModel>>(fees);

                foreach (var item in model)
                {
                    await PopulateDisplayNames(item);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deposit processing fees");
                TempData["Error"] = "An error occurred while retrieving deposit processing fees. Please try again.";
                return View(new List<DepositProcessingFeeViewModel>());
            }
        }

        // GET: DepositProcessingFees/Details/5
        [Breadcrumb("Details", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _depositProcessingFeeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var fee = await _depositProcessingFeeRepository.FindById(id.Value);
                if (fee == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DepositProcessingFeeViewModel>(fee);
                await PopulateDisplayNames(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deposit processing fee with id {DepositProcessingFeeId}", id);
                TempData["Error"] = "An error occurred while retrieving the deposit processing fee. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: DepositProcessingFees/Create
        [Breadcrumb("Create", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new DepositProcessingFeeViewModel();
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading denomination types for create");
                TempData["Error"] = "An error occurred while loading denomination types. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DepositProcessingFees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(DepositProcessingFeeViewModel model)
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

                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var fee = _mapper.Map<DepositProcessingFee>(model);
                bool isCreated = await _depositProcessingFeeRepository.Create(fee);

                if (isCreated)
                {
                    TempData["Success"] = "Deposit processing fee created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create deposit processing fee. Please try again.";
                    _logger.LogError("Error creating deposit processing fee");
                    ModelState.AddModelError("", "Failed to create deposit processing fee. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deposit processing fee");
                ModelState.AddModelError("", "Failed to create deposit processing fee. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: DepositProcessingFees/Edit/5
        [Breadcrumb("Edit", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _depositProcessingFeeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var fee = await _depositProcessingFeeRepository.FindById(id.Value);
                if (fee == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DepositProcessingFeeViewModel>(fee);
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deposit processing fee for edit with id {DepositProcessingFeeId}", id);
                TempData["Error"] = "An error occurred while retrieving the deposit processing fee. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DepositProcessingFees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, DepositProcessingFeeViewModel model)
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

                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var fee = await _depositProcessingFeeRepository.FindById(id);
                if (fee == null)
                {
                    return NotFound();
                }

                fee.StartValue = model.StartValue;
                fee.EndValue = model.EndValue;
                fee.MedianValue = model.MedianValue;
                fee.Rate = model.Rate;
                fee.Charge = model.Charge;
                fee.DenominationTypeId = model.DenominationTypeId;

                bool isUpdated = await _depositProcessingFeeRepository.Update(fee);
                if (isUpdated)
                {
                    TempData["Success"] = "Deposit processing fee updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update deposit processing fee. Please try again.";
                    ModelState.AddModelError("", "Failed to update deposit processing fee. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deposit processing fee with id {DepositProcessingFeeId}", id);
                ModelState.AddModelError("", "Failed to update deposit processing fee. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: DepositProcessingFees/Delete/5
        [Breadcrumb("Delete", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _depositProcessingFeeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var fee = await _depositProcessingFeeRepository.FindById(id.Value);
                if (fee == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<DepositProcessingFeeViewModel>(fee);
                await PopulateDisplayNames(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deposit processing fee for deletion with id {DepositProcessingFeeId}", id);
                TempData["Error"] = "An error occurred while retrieving the deposit processing fee. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DepositProcessingFees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(DepositProcessingFeesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _depositProcessingFeeRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var fee = await _depositProcessingFeeRepository.FindById(id);
                if (fee != null)
                {
                    fee.IsDeleted = true;
                    bool isDeleted = await _depositProcessingFeeRepository.Update(fee);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Deposit processing fee deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete deposit processing fee. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deposit processing fee with id {DepositProcessingFeeId}", id);
                TempData["Error"] = "An error occurred while deleting the deposit processing fee. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}