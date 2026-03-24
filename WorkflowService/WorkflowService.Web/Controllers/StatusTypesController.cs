using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SmartBreadcrumbs.Attributes;
using WorkflowService.Core.Models.StatusTypes;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Repositories.StatusTypes;
using WorkflowService.Core.Services.Identity;
using WorkflowService.Data.Entities;
using WorkflowService.Web.Controllers;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class StatusTypesController(
        IStatusTypeRepository _statusTypeRepository,
        IEntityTypeRepository _entityTypeRepository,
        IIdentityService _identityService,
        IMapper _mapper,
        ILogger<StatusTypesController> _logger) : Controller
    {
        private async Task PopulateDropdowns(StatusTypeViewModel model)
        {
            try
            {
                var applications = await _identityService.GetApplicationsAsync();
                model.Applications = applications
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                    .ToList();

                var entityTypes = await _entityTypeRepository.FindAll();
                var entityTypesList = entityTypes.ToList();

                if (model.ApplicationId > 0)
                {
                    var filteredEntityTypes = entityTypesList
                        .Where(et => et.ApplicationId == model.ApplicationId)
                        .Select(et => new SelectListItem { Value = et.Id.ToString(), Text = et.Name })
                        .ToList();
                    model.EntityTypes = filteredEntityTypes;
                }
                else
                {
                    model.EntityTypes = entityTypesList
                        .Select(et => new SelectListItem { Value = et.Id.ToString(), Text = et.Name })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns");
            }
        }

        [Breadcrumb("Status Types", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: StatusTypes
        public async Task<IActionResult> Index()
        {
            try
            {
                var applications = await _identityService.GetApplicationsAsync();
                var statusTypes = (await _statusTypeRepository.FindAll());
                var model = _mapper.Map<List<StatusTypeViewModel>>(statusTypes);
                
                foreach (var statusType in model)
                {
                    var application = applications.Where(x => x.Id == statusType.EntityType.ApplicationId).FirstOrDefault();
                    statusType.ApplicationName = application == null ? "Unknown" : application.Name;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status types");
                TempData["Error"] = "An error occurred while retrieving status types. Please try again.";
                return View(new List<StatusTypeViewModel>());
            }
        }

        // GET: StatusTypes/Details/5
        [Breadcrumb("Details", FromController = typeof(StatusTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _statusTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var statusType = await _statusTypeRepository.FindById(id.Value);
                if (statusType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<StatusTypeViewModel>(statusType);
                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status type with id {StatusTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the status type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: StatusTypes/Create
        [Breadcrumb("Create", FromController = typeof(StatusTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var model = new StatusTypeViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: StatusTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(StatusTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(StatusTypeViewModel model)
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
                var statusType = _mapper.Map<StatusType>(model);
                bool isCreated = await _statusTypeRepository.Create(statusType);

                if (isCreated)
                {
                    TempData["Success"] = "Status type created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create status type. Please try again.";
                    _logger.LogError("Error creating status type");
                    ModelState.AddModelError("", "Failed to create status type. Please try again.");
                    await PopulateDropdowns(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating status type");
                ModelState.AddModelError("", "Failed to create status type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
                await PopulateDropdowns(model);
            }

            return View(model);
        }

        // GET: StatusTypes/Edit/5
        [Breadcrumb("Edit", FromController = typeof(StatusTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _statusTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var statusType = await _statusTypeRepository.FindById(id.Value);
                if (statusType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<StatusTypeViewModel>(statusType);
                await PopulateDropdowns(model);
                model.ApplicationId = model.EntityType.ApplicationId;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status type for edit with id {StatusTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the status type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: StatusTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(StatusTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, StatusTypeViewModel model)
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
                var statusType = await _statusTypeRepository.FindById(id);
                if (statusType == null)
                {
                    return NotFound();
                }

                statusType.Name = model.Name;
                statusType.EntityTypeId = model.EntityTypeId;

                bool isUpdated = await _statusTypeRepository.Update(statusType);
                if (isUpdated)
                {
                    TempData["Success"] = "Status type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update status type. Please try again.";
                    ModelState.AddModelError("", "Failed to update status type. Please try again.");
                    await PopulateDropdowns(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status type with id {StatusTypeId}", id);
                ModelState.AddModelError("", "Failed to update status type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
                await PopulateDropdowns(model);
            }

            return View(model);
        }

        // GET: StatusTypes/GetEntityTypesByApplication
        [HttpGet]
        public async Task<IActionResult> GetEntityTypesByApplication(int applicationId)
        {
            try
            {
                var entityTypes = await _entityTypeRepository.FindByApplicationId(applicationId);
                return Json(entityTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity types for application {ApplicationId}", applicationId);
                return Json(new List<object>());
            }
        }
    }
}