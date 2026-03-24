using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using WorkflowService.Core.Models.EntityTypes;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Services.Identity;
using WorkflowService.Data.Entities;

namespace WorkflowService.Web.Controllers
{
    [Authorize]
    public class EntityTypesController(
        IEntityTypeRepository _entityTypeRepository,
        IIdentityService _identityService,
        IMapper _mapper,
        ILogger<EntityTypesController> _logger) : Controller
    {
        private async Task PopulateApplicationsDropdown(EntityTypeViewModel model)
        {
            try
            {
                var applications = await _identityService.GetApplicationsAsync();
                model.ApplicationsList = applications
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating applications dropdown");
            }
        }

        [Breadcrumb("Entity Types", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: EntityTypes
        public async Task<IActionResult> Index()
        {
            try
            {
                var applications = await _identityService.GetApplicationsAsync();
                List<EntityTypeViewModel> model = _mapper.Map<List<EntityTypeViewModel>>(await _entityTypeRepository.FindAll());

                foreach(var entityType in model)
                {
                    //var application = await _identityService.GetApplicationByIdAsync(entityType.ApplicationId);
                    var application = applications.Where(x => x.Id == entityType.ApplicationId).FirstOrDefault();
                    entityType.ApplicationName = application == null ? "Unknown" : application.Name;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity types");
                TempData["Error"] = "An error occurred while retrieving entity types. Please try again.";
                return View(new List<EntityTypeViewModel>());
            }
        }

        // GET: EntityTypes/Details/5
        [Breadcrumb("Details", FromController = typeof(EntityTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _entityTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var entityType = await _entityTypeRepository.FindById(id.Value);
                if (entityType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<EntityTypeViewModel>(entityType);
                await PopulateApplicationsDropdown(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity type with id {EntityTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the entity type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EntityTypes/Create
        [Breadcrumb("Create", FromController = typeof(EntityTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            var model = new EntityTypeViewModel();
            await PopulateApplicationsDropdown(model);
            return View(model);
        }

        // POST: EntityTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create", FromController = typeof(EntityTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(EntityTypeViewModel model)
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
                await PopulateApplicationsDropdown(model);
                return View(model);
            }

            try
            {
                var entityType = _mapper.Map<EntityType>(model);
                bool isCreated = await _entityTypeRepository.Create(entityType);

                if (isCreated)
                {
                    TempData["Success"] = "Entity type created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to create entity type. Please try again.";
                    _logger.LogError("Error creating entity type");
                    ModelState.AddModelError("", "Failed to create entity type. Please try again.");
                    await PopulateApplicationsDropdown(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating entity type");
                ModelState.AddModelError("", "Failed to create entity type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
                await PopulateApplicationsDropdown(model);
            }

            return View(model);
        }

        // GET: EntityTypes/Edit/5
        [Breadcrumb("Edit", FromController = typeof(EntityTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _entityTypeRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var entityType = await _entityTypeRepository.FindById(id.Value);
                if (entityType == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<EntityTypeViewModel>(entityType);
                await PopulateApplicationsDropdown(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity type for edit with id {EntityTypeId}", id);
                TempData["Error"] = "An error occurred while retrieving the entity type. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EntityTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit", FromController = typeof(EntityTypesController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, EntityTypeViewModel model)
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
                await PopulateApplicationsDropdown(model);
                return View(model);
            }

            try
            {
                var entityType = await _entityTypeRepository.FindById(id);
                if (entityType == null)
                {
                    return NotFound();
                }

                entityType.Name = model.Name;
                entityType.ApplicationId = model.ApplicationId;

                bool isUpdated = await _entityTypeRepository.Update(entityType);
                if (isUpdated)
                {
                    TempData["Success"] = "Entity type updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Failed to update entity type. Please try again.";
                    ModelState.AddModelError("", "Failed to update entity type. Please try again.");
                    await PopulateApplicationsDropdown(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity type with id {EntityTypeId}", id);
                ModelState.AddModelError("", "Failed to update entity type. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
                await PopulateApplicationsDropdown(model);
            }

            return View(model);
        }
    }
}