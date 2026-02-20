using AutoMapper;
using IdentityService.Core.Models;
using IdentityService.Core.Repository.Applications;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Web.Controllers
{
    [Authorize]
    public class ApplicationsController(IApplicationRepository _appRepo, IMapper _mapper, ILogger<ApplicationsController> _logger) : Controller
    {
        // GET: Applications
        public async Task<IActionResult> Index()
        {
            List<ApplicationViewModel> model = _mapper.Map<List<ApplicationViewModel>>(await _appRepo.FindAll());
            return View(model);
        }

        // GET: Applications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            bool exists = await _appRepo.IsExists(id ?? 0);
            if (!exists)
                return NotFound();

            ApplicationViewModel model = _mapper.Map<ApplicationViewModel>(await _appRepo.FindById(id ?? 0));

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // GET: Applications/Create
        public IActionResult Create()
        {
            ApplicationViewModel model = new ApplicationViewModel();
            return View(model);
        }

        // POST: Applications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool isCreated = await _appRepo.Create(_mapper.Map<Application>(model));
                    if (isCreated)
                    {
                        TempData["Success"] = "Application added successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Failed to add Application. Something went Wrong. Please try again";
                        _logger.LogError("Error creating application");
                        ModelState.AddModelError("", "Failed to create application. Please try again");
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error creating application");
                    ModelState.AddModelError(ex.Message, "Failed to add Application. Something went wrong");
                }
            }
            return View(model);
        }

        // GET: Applications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ApplicationViewModel model = _mapper.Map<ApplicationViewModel>(await _appRepo.FindById(id ?? 0));
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        // POST: Applications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApplicationViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Application application = await _appRepo.FindById(id);
                    application.Name = model.Name;
                    application.Description = model.Description;
                    application.Acronym = model.Acronym;
                    application.Status = model.Status;
                    application.Url = model.Url;
                    bool updated = await _appRepo.Update(application);
                    if (updated)
                    {
                        TempData["Success"] = "Application updated successfully!";
                        return RedirectToAction(nameof(Index));
                    } else
                    {
                        TempData["Error"] = "Failed to update application. Please try again";
                        ModelState.AddModelError("", "Failed to update application. Please try again");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating application with id {ApplicationId}", id);
                    ModelState.AddModelError(ex.Message, "Failed to save changes. Something went wrong");
                }
            }
            return View(model);
        }

        // GET: Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = _mapper.Map<ApplicationViewModel>(await _appRepo.FindById(id ?? 0));
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _appRepo.FindById(id);

            // TO DO: Consider implementing a soft delete instead of permanently deleting the record
            // Add try catch, check if the application is associated with any users or roles before allowing deletion
            // If the application is associated with users or roles, prevent deletion and show an error message
            // If the application is not associated with any users or roles, proceed with deletion
            bool isDeleted = await _appRepo.Delete(application);
            if (isDeleted)
            {
                TempData["Success"] = "Application deleted successfully!";
            } else
            {
                TempData["Error"] = "Failed to delete application. Please try again";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
