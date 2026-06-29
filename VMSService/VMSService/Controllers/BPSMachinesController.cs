using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.BPSMachines;
using VMSService.Core.Repositories.BPSMachines;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class BPSMachinesController(
        IBPSMachineRepository _bpsMachineRepository,
        IMapper _mapper,
        ILogger<BPSMachinesController> _logger) : Controller
    {
        [Breadcrumb("BPS Machines", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var machines = await _bpsMachineRepository.FindAll();
                var model = _mapper.Map<List<BPSMachineViewModel>>(machines);
                foreach (var m in model)
                    m.CurrentStatusName = BPSMachineViewModel.GetStatusName(m.CurrentStatusId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving BPS machines");
                TempData["Error"] = "An error occurred while retrieving BPS machines. Please try again.";
                return View(new List<BPSMachineViewModel>());
            }
        }

        [Breadcrumb("Details", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _bpsMachineRepository.IsExists(id.Value)) return NotFound();

                var machine = await _bpsMachineRepository.FindById(id.Value);
                if (machine == null) return NotFound();

                var model = _mapper.Map<BPSMachineViewModel>(machine);
                model.CurrentStatusName = BPSMachineViewModel.GetStatusName(model.CurrentStatusId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving BPS machine with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the BPS machine. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Breadcrumb("Create", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new BPSMachineViewModel { StatusList = BPSMachineViewModel.GetStatusList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(BPSMachineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.StatusList = BPSMachineViewModel.GetStatusList();
                return View(model);
            }

            try
            {
                var machine = _mapper.Map<BPSMachine>(model);
                bool isCreated = await _bpsMachineRepository.Create(machine);

                if (isCreated)
                {
                    TempData["Success"] = "BPS machine created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create BPS machine. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating BPS machine");
                TempData["Error"] = ex.Message;
            }

            model.StatusList = BPSMachineViewModel.GetStatusList();
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _bpsMachineRepository.IsExists(id.Value)) return NotFound();

                var machine = await _bpsMachineRepository.FindById(id.Value);
                if (machine == null) return NotFound();

                var model = _mapper.Map<BPSMachineViewModel>(machine);
                model.StatusList = BPSMachineViewModel.GetStatusList();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving BPS machine for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the BPS machine. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, BPSMachineViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                model.StatusList = BPSMachineViewModel.GetStatusList();
                return View(model);
            }

            try
            {
                var machine = await _bpsMachineRepository.FindById(id);
                if (machine == null) return NotFound();

                machine.Name = model.Name;
                machine.CurrentStatusId = model.CurrentStatusId;

                bool isUpdated = await _bpsMachineRepository.Update(machine);
                if (isUpdated)
                {
                    TempData["Success"] = "BPS machine updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update BPS machine. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating BPS machine with id {Id}", id);
                TempData["Error"] = ex.Message;
            }

            model.StatusList = BPSMachineViewModel.GetStatusList();
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                if (!await _bpsMachineRepository.IsExists(id.Value)) return NotFound();

                var machine = await _bpsMachineRepository.FindById(id.Value);
                if (machine == null) return NotFound();

                var model = _mapper.Map<BPSMachineViewModel>(machine);
                model.CurrentStatusName = BPSMachineViewModel.GetStatusName(model.CurrentStatusId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving BPS machine for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the BPS machine. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(BPSMachinesController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (!await _bpsMachineRepository.IsExists(id)) return NotFound();

                var machine = await _bpsMachineRepository.FindById(id);
                if (machine == null) return NotFound();

                machine.IsDeleted = true;
                bool isDeleted = await _bpsMachineRepository.Update(machine);

                TempData[isDeleted ? "Success" : "Error"] = isDeleted
                    ? "BPS machine deleted successfully!"
                    : "Failed to delete BPS machine. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting BPS machine with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the BPS machine. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
