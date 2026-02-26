using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using VMSService.Core.Models.StatusTypes;
using VMSService.Core.Models.WorkflowTransitions;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.WorkflowTransitions;
using VMSService.Data.Entities;

namespace VMSService.Web.Controllers
{
    //[Authorize]
    public class WorkflowTransitionsController : Controller
    {
        private readonly IWorkflowTransitionRepository _workflowTransitionRepository;
        private readonly IStatusTypeRepository _statusTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<WorkflowTransitionsController> _logger;

        public WorkflowTransitionsController(
            IWorkflowTransitionRepository workflowTransitionRepository,
            IStatusTypeRepository statusTypeRepository,
            IMapper mapper,
            ILogger<WorkflowTransitionsController> logger)
        {
            _workflowTransitionRepository = workflowTransitionRepository;
            _statusTypeRepository = statusTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        [Breadcrumb("Workflow Transitions")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var transitions = await _workflowTransitionRepository.FindAll();
                var model = _mapper.Map<List<WorkflowTransitionViewModel>>(transitions);
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
        public async Task<IActionResult> Create()
        {
            try
            {
                var statuses = await _statusTypeRepository.FindAll();
                ViewBag.AllStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ButtonClasses = new List<string> 
                { 
                    "btn-success", 
                    "btn-danger", 
                    "btn-warning", 
                    "btn-info", 
                    "btn-primary",
                    "btn-secondary"
                };

                return View(new WorkflowTransitionViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create view");
                TempData["Error"] = "An error occurred while loading the form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Create Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create(WorkflowTransitionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    _logger.LogWarning("ModelState Error: {ErrorMessage}", error.ErrorMessage);
                }
                
                var statuses = await _statusTypeRepository.FindAll();
                ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ButtonClasses = new List<string> 
                { 
                    "btn-success", 
                    "btn-danger", 
                    "btn-warning", 
                    "btn-info", 
                    "btn-primary",
                    "btn-secondary"
                };
                
                return View(model);
            }

            try
            {
                // Validate that from and to statuses are different
                if (model.FromStatusId == model.ToStatusId)
                {
                    ModelState.AddModelError("", "From Status and To Status must be different.");
                    var statuses = await _statusTypeRepository.FindAll();
                    ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                    ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                    ViewBag.ButtonClasses = new List<string> 
                    { 
                        "btn-success", 
                        "btn-danger", 
                        "btn-warning", 
                        "btn-info", 
                        "btn-primary",
                        "btn-secondary"
                    };
                    return View(model);
                }

                //var workflowTransition = _mapper.Map<WorkflowTransition>(model);
                //workflowTransition.IsActive = true;
                //workflowTransition.IsDeleted = false;
                WorkflowTransition workflowTransition = new()
                {
                    EntityType = model.EntityType,
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
                    return RedirectToAction(nameof(Index));
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

            var statusesList = await _statusTypeRepository.FindAll();
            ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statusesList);
            ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statusesList);
            ViewBag.ButtonClasses = new List<string> 
            { 
                "btn-success", 
                "btn-danger", 
                "btn-warning", 
                "btn-info", 
                "btn-primary",
                "btn-secondary"
            };

            return View(model);
        }

        [Breadcrumb("Edit Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;

                var statuses = await _statusTypeRepository.FindAll();
                ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ButtonClasses = new List<string> 
                { 
                    "btn-success", 
                    "btn-danger", 
                    "btn-warning", 
                    "btn-info", 
                    "btn-primary",
                    "btn-secondary"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition for edit with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Edit Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id, WorkflowTransitionViewModel model)
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

                var statuses = await _statusTypeRepository.FindAll();
                ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                ViewBag.ButtonClasses = new List<string> 
                { 
                    "btn-success", 
                    "btn-danger", 
                    "btn-warning", 
                    "btn-info", 
                    "btn-primary",
                    "btn-secondary"
                };
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
                    var statuses = await _statusTypeRepository.FindAll();
                    ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                    ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statuses);
                    ViewBag.ButtonClasses = new List<string> 
                    { 
                        "btn-success", 
                        "btn-danger", 
                        "btn-warning", 
                        "btn-info", 
                        "btn-primary",
                        "btn-secondary"
                    };
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
                    return RedirectToAction(nameof(Index));
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

            var statusesList = await _statusTypeRepository.FindAll();
            ViewBag.FromStatuses = _mapper.Map<List<StatusTypeViewModel>>(statusesList);
            ViewBag.ToStatuses = _mapper.Map<List<StatusTypeViewModel>>(statusesList);
            ViewBag.ButtonClasses = new List<string> 
            { 
                "btn-success", 
                "btn-danger", 
                "btn-warning", 
                "btn-info", 
                "btn-primary",
                "btn-secondary"
            };

            return View(model);
        }

        [Breadcrumb("Delete Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition for deletion with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete Transition", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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

            return RedirectToAction(nameof(Index));
        }

        [Breadcrumb("Details", FromController = typeof(WorkflowTransitionsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var transition = await _workflowTransitionRepository.FindById(id.Value);
                if (transition == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<WorkflowTransitionViewModel>(transition);
                model.FromStatusName = transition.FromStatus?.Name;
                model.ToStatusName = transition.ToStatus?.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow transition details with id {WorkflowTransitionId}", id);
                TempData["Error"] = "An error occurred while retrieving the workflow transition. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}