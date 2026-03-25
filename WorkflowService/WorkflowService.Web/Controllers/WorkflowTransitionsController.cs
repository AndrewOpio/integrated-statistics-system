using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using WorkflowService.Core.Models.WorkflowTransitions;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Repositories.StatusTypes;
using WorkflowService.Core.Repositories.WorkflowTransitions;
using WorkflowService.Core.Services.Identity;
using WorkflowService.Data.Entities;

namespace WorkflowService.Web.Controllers
{
    [Authorize]
    public class WorkflowTransitionsController(IWorkflowTransitionRepository _workflowTransitionRepository,
        IStatusTypeRepository _statusTypeRepository,
        IEntityTypeRepository _entityTypeRepository,
        IMapper _mapper,
        ILogger<WorkflowTransitionsController> _logger,
        IIdentityService _identityService) : Controller
    {
        private async Task PopulateDropdowns(WorkflowTransitionViewModel model, int applicationId)
        {
            try
            {
                // Get EntityTypes for the application
                var entityTypes = await _entityTypeRepository.FindByApplicationId(applicationId);
                model.EntityTypesList = entityTypes
                    .Select(et => new SelectListItem { Value = et.Id.ToString(), Text = et.Name })
                    .ToList();

                // Get StatusTypes for the application
                var statuses = await _statusTypeRepository.FindByApplicationId(applicationId);
                model.FromStatusesList = statuses
                    .Select(s => new SelectListItem 
                    { 
                        Value = s.Id.ToString(), 
                        Text = $"{s.Name} ({s.EntityType?.Name})"
                    })
                    .ToList();

                model.ToStatusesList = statuses
                    .Select(s => new SelectListItem 
                    { 
                        Value = s.Id.ToString(), 
                        Text = $"{s.Name} ({s.EntityType?.Name})"
                    })
                    .ToList();

                // Get Roles for the application
                var roles = await _identityService.GetRolesForApplicationAsync(applicationId);
                model.RequiredRoleList = roles
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = $"{s.Role.Name}"
                    })
                    .ToList();

                ViewBag.ButtonClasses = new List<string>
                { 
                    "btn-success", 
                    "btn-danger", 
                    "btn-warning", 
                    "btn-info", 
                    "btn-primary",
                    "btn-secondary"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns for applicationId {ApplicationId}", applicationId);
            }
        }

        [Breadcrumb("Workflow Transitions")]
        public async Task<IActionResult> Index(int applicationId)
        {
            // Set custom breadcrumb with query parameter
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId }))
            };
            try
            {
                ViewBag.ApplicationName = "";
                ViewBag.ApplicationId = 0;
                var application = await _identityService.GetApplicationByIdAsync(applicationId);
                if(application != null)
                {
                    ViewBag.ApplicationName = application.Name;
                    ViewBag.ApplicationId = applicationId;
                }

                var transitions = await _workflowTransitionRepository.FindByApplicationId(applicationId);
                var model = _mapper.Map<List<WorkflowTransitionViewModel>>(transitions);
                foreach (var transition in model)
                {
                    var appRole = await _identityService.GetRoleByIdAsync(transition.RequiredRole);
                    transition.RequiredRoleName = appRole.Role.Name;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transitions");
                TempData["Error"] = "An error occurred while retrieving workflow transitions. Please try again.";
                return View(new List<WorkflowTransitionViewModel>());
            }
        }

        [Breadcrumb("Create Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(int applicationId)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Create Transition", Url.Action("Create", "WorkflowTransitions", new { applicationId }))
            };
            try
            {
                ViewBag.ApplicationId = applicationId;
                var model = new WorkflowTransitionViewModel();
                await PopulateDropdowns(model, applicationId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create view");
                TempData["Error"] = "An error occurred while loading the form. Please try again.";
                return RedirectToAction(nameof(Index), new { applicationId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(int applicationId, WorkflowTransitionViewModel model)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Create Transition", Url.Action("Create", "WorkflowTransitions", new { applicationId }))
            };
            ViewBag.ApplicationId = applicationId;
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }
                
                await PopulateDropdowns(model, applicationId);
                return View(model);
            }

            try
            {
                // Validate that from and to statuses are different
                if (model.FromStatusId == model.ToStatusId)
                {
                    ModelState.AddModelError("", "From Status and To Status must be different.");
                    await PopulateDropdowns(model, applicationId);
                    return View(model);
                }

                WorkflowTransition workflowTransition = new()
                {
                    EntityTypeId = model.EntityTypeId,
                    FromStatusId = model.FromStatusId,
                    ToStatusId = model.ToStatusId,
                    ActionName = model.ActionName,
                    RequiredRole = model.RequiredRole,
                    RequiresComments = model.RequiresComments,
                    DisplayLabel = model.DisplayLabel,
                    ButtonClass = model.ButtonClass,
                    IsActive = true,
                    IsDeleted = false
                };

                bool isCreated = await _workflowTransitionRepository.Create(workflowTransition);

                if (isCreated)
                {
                    TempData["Success"] = "Workflow transition created successfully!";
                    return RedirectToAction(nameof(Index), new { applicationId });
                }
                else
                {
                    TempData["Error"] = "Failed to create workflow transition. Please try again.";
                    _logger.LogError("Error creating workflow transition");
                    ModelState.AddModelError("", "Failed to create workflow transition. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow transition");
                ModelState.AddModelError("", "Failed to create workflow transition. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model, applicationId);
            return View(model);
        }

        [Breadcrumb("Edit Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id, int applicationId)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Edit Transition", Url.Action("Edit", "WorkflowTransitions", new { id, applicationId }))
            };
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                ViewBag.ApplicationId = applicationId;
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;
                model.EntityTypeName = transition.EntityType?.Name;

                var appRole = await _identityService.GetRoleByIdAsync(model.RequiredRole);
                model.RequiredRoleName = appRole.Role.Name;

                await PopulateDropdowns(model, applicationId);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition for edit with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index), new { applicationId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, int applicationId, WorkflowTransitionViewModel model)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Edit Transition", Url.Action("Edit", "WorkflowTransitions", new { id, applicationId }))
            };
            if (id != model.Id)
            {
                return NotFound();
            }

            ViewBag.ApplicationId = applicationId;
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                    ModelState.AddModelError("", error.ErrorMessage);
                }
                ModelState.AddModelError("", "Please correct the errors and try again.");

                await PopulateDropdowns(model, applicationId);
                return View(model);
            }

            try
            {
                var transition = await _workflowTransitionRepository.FindById(id);
                if (transition == null)
                {
                    return NotFound();
                }

                // Validate that from and to statuses are different
                if (model.FromStatusId == model.ToStatusId)
                {
                    ModelState.AddModelError("", "From Status and To Status must be different.");
                    await PopulateDropdowns(model, applicationId);
                    return View(model);
                }

                // Update properties
                transition.ActionName = model.ActionName;
                transition.DisplayLabel = model.DisplayLabel;
                transition.RequiredRole = model.RequiredRole;
                transition.RequiresComments = model.RequiresComments;
                transition.ButtonClass = model.ButtonClass;
                transition.IsActive = model.IsActive;

                bool isUpdated = await _workflowTransitionRepository.Update(transition);

                if (isUpdated)
                {
                    TempData["Success"] = "Workflow transition updated successfully!";
                    return RedirectToAction(nameof(Index), new { applicationId });
                }
                else
                {
                    TempData["Error"] = "Failed to update workflow transition. Please try again.";
                    ModelState.AddModelError("", "Failed to update workflow transition. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow transition with id {WorkflowTransitionId}", id);
                ModelState.AddModelError("", "Failed to update workflow transition. An unexpected error occurred.");
                TempData["Error"] = ex.Message;
            }

            await PopulateDropdowns(model, applicationId);
            return View(model);
        }

        [Breadcrumb("Delete Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id, int applicationId)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Delete Transition", Url.Action("Delete", "WorkflowTransitions", new { id, applicationId }))
            };

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                ViewBag.ApplicationId = applicationId;
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;
                model.EntityTypeName = transition.EntityType?.Name;

                var appRole = await _identityService.GetRoleByIdAsync(model.RequiredRole);
                model.RequiredRoleName = appRole.Role.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition for deletion with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index), new { applicationId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id, int applicationId)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Delete Transition", Url.Action("Delete", "WorkflowTransitions", new { id, applicationId }))
            };
            try
            {
                var transition = await _workflowTransitionRepository.FindById(id);
                if (transition == null)
                {
                    return NotFound();
                }

                // Soft delete
                transition.IsDeleted = true;
                bool isDeleted = await _workflowTransitionRepository.Update(transition);

                if (isDeleted)
                {
                    TempData["Success"] = "Workflow transition deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete workflow transition. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow transition with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while deleting the workflow transition. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { applicationId });
        }

        [Breadcrumb("Details", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id, int applicationId)
        {
            // Set custom breadcrumb with query parameters
            ViewBag.BreadcrumbItems = new List<(string Text, string Url)>
            {
                ("Home", Url.Action("Index", "Home")),
                ("Workflow Transitions", Url.Action("Index", "WorkflowTransitions", new { applicationId })),
                ("Details", Url.Action("Details", "WorkflowTransitions", new { id, applicationId }))
            };
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                ViewBag.ApplicationId = applicationId;
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;
                model.EntityTypeName = transition.EntityType?.Name;

                var appRole = await _identityService.GetRoleByIdAsync(model.RequiredRole);
                model.RequiredRoleName = appRole.Role.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition details with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index), new { applicationId });
            }
        }
    }
}