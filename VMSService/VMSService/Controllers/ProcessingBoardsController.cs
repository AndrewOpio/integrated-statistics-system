using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.CountingRooms;
using VMSService.Core.Models.ProcessingBoards;
using VMSService.Core.Repositories.BPSMachines;
using VMSService.Core.Repositories.CountingRooms;
using VMSService.Core.Repositories.ProcessingBoards;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class ProcessingBoardsController(
        IProcessingBoardRepository _processingBoardRepository,
        IBPSMachineRepository _bpsMachineRepository,
        ICountingRoomRepository _countingRoomRepository,
        IConfigurationsServiceClient _configServiceClient,
        IMapper _mapper,
        ILogger<ProcessingBoardsController> _logger) : Controller
    {
        [Breadcrumb("Processing Boards", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var boards = await _processingBoardRepository.FindAll();
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                var model = _mapper.Map<List<ProcessingBoardViewModel>>(boards);
                var branchDict = branches.ToDictionary(b => b.Id, b => b.Name);
                foreach (var item in model)
                {
                    if (branchDict.TryGetValue(item.BranchId, out var branchName))
                        item.BranchName = branchName;
                }
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving processing boards");
                TempData["Error"] = "An error occurred while retrieving processing boards. Please try again.";
                return View(new List<ProcessingBoardViewModel>());
            }
        }

        [Breadcrumb("Details", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _processingBoardRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var board = await _processingBoardRepository.FindById(id.Value);
                if (board == null) return NotFound();

                var model = _mapper.Map<ProcessingBoardViewModel>(board);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving processing board with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the processing board. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Breadcrumb("Create", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new ProcessingBoardViewModel();
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns for create");
                TempData["Error"] = "An error occurred while loading form data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(ProcessingBoardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var board = _mapper.Map<ProcessingBoard>(model);
                bool isCreated = await _processingBoardRepository.Create(board);

                if (isCreated)
                {
                    TempData["Success"] = "Processing board created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create processing board. Please try again.";
                ModelState.AddModelError("", "Failed to create processing board. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating processing board");
                ModelState.AddModelError("", "Failed to create processing board. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _processingBoardRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var board = await _processingBoardRepository.FindById(id.Value);
                if (board == null) return NotFound();

                var model = _mapper.Map<ProcessingBoardViewModel>(board);
                await PopulateDropdowns(model, board.Id);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving processing board for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the processing board. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, ProcessingBoardViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateDropdowns(model, model.Id);
                return View(model);
            }

            try
            {
                var board = await _processingBoardRepository.FindById(id);
                if (board == null) return NotFound();

                board.Name = model.Name;
                board.BranchId = model.BranchId;
                board.BPSMachineId = model.BPSMachineId;
                board.CountingRoomId = model.CountingRoomId;

                bool isUpdated = await _processingBoardRepository.Update(board);
                if (isUpdated)
                {
                    TempData["Success"] = "Processing board updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update processing board. Please try again.";
                ModelState.AddModelError("", "Failed to update processing board. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating processing board with id {Id}", id);
                ModelState.AddModelError("", "Failed to update processing board. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model, model.Id);
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _processingBoardRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var board = await _processingBoardRepository.FindById(id.Value);
                if (board == null) return NotFound();

                var model = _mapper.Map<ProcessingBoardViewModel>(board);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving processing board for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the processing board. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(ProcessingBoardsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _processingBoardRepository.IsExists(id);
                if (!exists) return NotFound();

                var board = await _processingBoardRepository.FindById(id);
                if (board != null)
                {
                    board.IsDeleted = true;
                    bool isDeleted = await _processingBoardRepository.Update(board);
                    TempData[isDeleted ? "Success" : "Error"] = isDeleted
                        ? "Processing board deleted successfully!"
                        : "Failed to delete processing board. Please try again.";
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting processing board with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the processing board. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ProcessingBoardViewModel model, int? currentBoardId = null)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BranchList = branches
                .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
                .ToList();

            var machines = await _bpsMachineRepository.FindAll();
            var boards = await _processingBoardRepository.FindAll();
            var usedMachineIds = boards
                .Where(b => b.Id != currentBoardId && b.BPSMachineId.HasValue)
                .Select(b => b.BPSMachineId!.Value)
                .ToHashSet();
            model.BPSMachineList = machines
                .Where(m => m.CurrentStatusId == 1 && !usedMachineIds.Contains(m.Id))
                .Select(m => new SelectListItem(m.Name, m.Id.ToString()))
                .ToList();

            var rooms = await _countingRoomRepository.FindAll();
            model.CountingRoomList = rooms
                .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
                .ToList();
            model.CountingRooms = rooms
                .Select(r => new CountingRoomViewModel { Id = r.Id, Name = r.Name, BranchId = r.BranchId })
                .ToList();
        }
    }
}
