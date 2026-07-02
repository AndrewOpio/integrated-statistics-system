using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.CountingRooms;
using VMSService.Core.Repositories.CountingRooms;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class CountingRoomsController(
        ICountingRoomRepository _countingRoomRepository,
        IConfigurationsServiceClient _configServiceClient,
        IMapper _mapper,
        ILogger<CountingRoomsController> _logger) : Controller
    {
        [Breadcrumb("Counting Rooms", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var rooms = await _countingRoomRepository.FindAll();
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                var model = _mapper.Map<List<CountingRoomViewModel>>(rooms);
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
                _logger.LogError(ex, "Error retrieving counting rooms");
                TempData["Error"] = "An error occurred while retrieving counting rooms. Please try again.";
                return View(new List<CountingRoomViewModel>());
            }
        }

        [Breadcrumb("Details", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _countingRoomRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var room = await _countingRoomRepository.FindById(id.Value);
                if (room == null) return NotFound();

                var model = _mapper.Map<CountingRoomViewModel>(room);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counting room with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the counting room. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Breadcrumb("Create", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new CountingRoomViewModel();
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for create");
                TempData["Error"] = "An error occurred while loading form data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(CountingRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateBranchList(model);
                return View(model);
            }

            try
            {
                var room = _mapper.Map<CountingRoom>(model);
                bool isCreated = await _countingRoomRepository.Create(room);

                if (isCreated)
                {
                    TempData["Success"] = "Counting room created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create counting room. Please try again.";
                ModelState.AddModelError("", "Failed to create counting room. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating counting room");
                ModelState.AddModelError("", "Failed to create counting room. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _countingRoomRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var room = await _countingRoomRepository.FindById(id.Value);
                if (room == null) return NotFound();

                var model = _mapper.Map<CountingRoomViewModel>(room);
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counting room for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the counting room. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, CountingRoomViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateBranchList(model);
                return View(model);
            }

            try
            {
                var room = await _countingRoomRepository.FindById(id);
                if (room == null) return NotFound();

                room.Name = model.Name;
                room.BranchId = model.BranchId;

                bool isUpdated = await _countingRoomRepository.Update(room);
                if (isUpdated)
                {
                    TempData["Success"] = "Counting room updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update counting room. Please try again.";
                ModelState.AddModelError("", "Failed to update counting room. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating counting room with id {Id}", id);
                ModelState.AddModelError("", "Failed to update counting room. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _countingRoomRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var room = await _countingRoomRepository.FindById(id.Value);
                if (room == null) return NotFound();

                var model = _mapper.Map<CountingRoomViewModel>(room);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counting room for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the counting room. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(CountingRoomsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _countingRoomRepository.IsExists(id);
                if (!exists) return NotFound();

                var room = await _countingRoomRepository.FindById(id);
                if (room != null)
                {
                    room.IsDeleted = true;
                    bool isDeleted = await _countingRoomRepository.Update(room);
                    TempData[isDeleted ? "Success" : "Error"] = isDeleted
                        ? "Counting room deleted successfully!"
                        : "Failed to delete counting room. Please try again.";
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting counting room with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the counting room. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBranchList(CountingRoomViewModel model)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BranchList = branches
                .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
                .ToList();
        }
    }
}
