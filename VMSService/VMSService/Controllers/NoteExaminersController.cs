using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using VMSService.Controllers;
using VMSService.Core.Models.NoteExaminers;
using VMSService.Core.Repositories.NoteExaminers;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class NoteExaminersController(
        INoteExaminerRepository _noteExaminerRepository,
        IConfigurationsServiceClient _configServiceClient,
        IMapper _mapper,
        ILogger<NoteExaminersController> _logger) : Controller
    {
        [Breadcrumb("Note Examiners", FromController = typeof(HomeController), FromAction = nameof(Index))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var examiners = await _noteExaminerRepository.FindAll();
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                var model = _mapper.Map<List<NoteExaminerViewModel>>(examiners);
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
                _logger.LogError(ex, "Error retrieving note examiners");
                TempData["Error"] = "An error occurred while retrieving note examiners. Please try again.";
                return View(new List<NoteExaminerViewModel>());
            }
        }

        [Breadcrumb("Details", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _noteExaminerRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var examiner = await _noteExaminerRepository.FindById(id.Value);
                if (examiner == null) return NotFound();

                var model = _mapper.Map<NoteExaminerViewModel>(examiner);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note examiner with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the note examiner. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Breadcrumb("Create", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new NoteExaminerViewModel();
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for create note examiner");
                TempData["Error"] = "An error occurred while loading form data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(NoteExaminerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Please correct the errors and try again.");
                await PopulateBranchList(model);
                return View(model);
            }

            try
            {
                var examiner = _mapper.Map<NoteExaminer>(model);
                bool isCreated = await _noteExaminerRepository.Create(examiner);

                if (isCreated)
                {
                    TempData["Success"] = "Note examiner created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to create note examiner. Please try again.";
                ModelState.AddModelError("", "Failed to create note examiner. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note examiner");
                ModelState.AddModelError("", "Failed to create note examiner. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Edit", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _noteExaminerRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var examiner = await _noteExaminerRepository.FindById(id.Value);
                if (examiner == null) return NotFound();

                var model = _mapper.Map<NoteExaminerViewModel>(examiner);
                await PopulateBranchList(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note examiner for edit with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the note examiner. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, NoteExaminerViewModel model)
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
                var examiner = await _noteExaminerRepository.FindById(id);
                if (examiner == null) return NotFound();

                examiner.FirstName = model.FirstName;
                examiner.MiddleName = model.MiddleName;
                examiner.LastName = model.LastName;
                examiner.BranchId = model.BranchId;
                examiner.IsActive = model.IsActive;

                bool isUpdated = await _noteExaminerRepository.Update(examiner);
                if (isUpdated)
                {
                    TempData["Success"] = "Note examiner updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Failed to update note examiner. Please try again.";
                ModelState.AddModelError("", "Failed to update note examiner. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note examiner with id {Id}", id);
                ModelState.AddModelError("", "Failed to update note examiner. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateBranchList(model);
            return View(model);
        }

        [Breadcrumb("Delete", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                bool exists = await _noteExaminerRepository.IsExists(id.Value);
                if (!exists) return NotFound();

                var examiner = await _noteExaminerRepository.FindById(id.Value);
                if (examiner == null) return NotFound();

                var model = _mapper.Map<NoteExaminerViewModel>(examiner);
                var branches = await _configServiceClient.GetAllBouBranchesAsync();
                model.BranchName = branches.FirstOrDefault(b => b.Id == model.BranchId)?.Name;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note examiner for deletion with id {Id}", id);
                TempData["Error"] = "An error occurred while retrieving the note examiner. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(NoteExaminersController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _noteExaminerRepository.IsExists(id);
                if (!exists) return NotFound();

                var examiner = await _noteExaminerRepository.FindById(id);
                if (examiner != null)
                {
                    examiner.IsDeleted = true;
                    bool isDeleted = await _noteExaminerRepository.Update(examiner);
                    TempData[isDeleted ? "Success" : "Error"] = isDeleted
                        ? "Note examiner deleted successfully!"
                        : "Failed to delete note examiner. Please try again.";
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note examiner with id {Id}", id);
                TempData["Error"] = "An error occurred while deleting the note examiner. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBranchList(NoteExaminerViewModel model)
        {
            var branches = await _configServiceClient.GetAllBouBranchesAsync();
            model.BranchList = branches
                .Select(b => new SelectListItem(b.Name, b.Id.ToString()))
                .ToList();
        }
    }
}
