using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Controllers;
using VMSService.Core.Repositories.Logistics;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Web.Controllers
{
    [Authorize]
    public class LogisticsController : Controller
    {
        private readonly ILogisticsRepository _logisticsRepository;
        private readonly ILogger<LogisticsController> _logger;
        private readonly VMSService.Core.Services.Workflows.IWorkflowService _workflowService;
        private readonly ILogisticsStatusHistoryRepository _statusHistoryRepository;
        private readonly IWorkflowServiceClient _workflowServiceClient;

        public LogisticsController(
            ILogisticsRepository logisticsRepository,
            ILogger<LogisticsController> logger,
            VMSService.Core.Services.Workflows.IWorkflowService workflowService,
            ILogisticsStatusHistoryRepository statusHistoryRepository,
            IWorkflowServiceClient workflowServiceClient)
        {
            _logisticsRepository = logisticsRepository;
            _logger = logger;
            _workflowService = workflowService;
            _statusHistoryRepository = statusHistoryRepository;
            _workflowServiceClient = workflowServiceClient;
        }

        [Breadcrumb("Logistics", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public async Task<IActionResult> Index()
        {
            var records = await _logisticsRepository.GetAllAsync();
            foreach (var record in records)
            {
                record.CurrentStatusName = (await _workflowServiceClient.FindStatusById(record.CurrentStatusId)).Name;
            }
            return View(records);
        }

        [HttpGet]
        [Breadcrumb("Create", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public IActionResult Create()
        {
            return View(new Data.Entities.Logistics());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] LogisticsCreateViewModel model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid request body." });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserName = User.FindFirstValue("displayName") ?? User.Identity?.Name ?? "Unknown";



            // Fetch the Draft status for Logistics
            var draftStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.Logistics.Draft,
                EntityTypeName = StatusConstants.Entity.Logistics
            });

            var logistics = new Data.Entities.Logistics
            {
                Title = model.Title,
                Description = model.Description,
                ReferenceNumber = model.ReferenceNumber,
                Date = model.Date,
                TeamLeader = model.TeamLeader,
                AssistantTeamLeader = model.AssistantTeamLeader,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow,
                CurrentStatusId = draftStatus.Id,
                Categories = model.Categories.Select((cat, catIndex) => new LogisticsCategory
                {
                    Name = cat.Name,
                    SortOrder = catIndex,
                    Details = cat.Details.Select((det, detIndex) => new LogisticsDetail
                    {
                        ItemName = det.ItemName,
                        Description = det.Description,
                        UnitCost = det.UnitCost,
                        Quantity = det.Quantity,
                        Frequency = det.Frequency,
                        TotalCost = det.TotalCost,
                        Notes = det.Notes,
                        SortOrder = detIndex
                    }).ToList()
                }).ToList()
            };

            await _logisticsRepository.CreateAsync(logistics);

            return Json(new { success = true, logisticsId = logistics.Id, message = "Logistics record created successfully." });
        }

        [HttpGet]
        [Breadcrumb("Details", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int id)
        {
            var logistics = await _logisticsRepository.GetByIdAsync(id);
            if (logistics == null)
                return NotFound();

            // Fetch available workflow transitions for this logistics record using user roles (controller logic, not service)
            var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

            var availableTransitions = new List<Core.Models.Logistics.WorkflowTransitionDto>();

            foreach (var userRole in userRoles)
            {
                var transitions = await _workflowServiceClient.GetAvailableTransitionsAsync(
                    StatusConstants.Entity.Logistics, 
                    logistics.CurrentStatusId,
                    userRole);

                if (transitions != null)
                {
                    availableTransitions.AddRange(transitions.Select(t => new Core.Models.Logistics.WorkflowTransitionDto
                    {
                        ActionName = t.ActionName,
                        DisplayLabel = t.DisplayLabel,
                        RequiresComments = t.RequiresComments,
                        ButtonClass = t.ButtonClass
                    }));
                }
            }

            ViewBag.AvailableTransitions = availableTransitions;

            // Fetch status history via repository (not navigation property)
            var statusHistory = await _statusHistoryRepository.GetByLogisticsIdAsync(id);
            foreach (var history in statusHistory)
            {
                history.OldStatusName = (await _workflowServiceClient.FindStatusById(history.OldStatusId)).Name;
                history.NewStatusName = (await _workflowServiceClient.FindStatusById(history.NewStatusId)).Name;
            }

            ViewBag.StatusHistory = statusHistory ?? new List<VMSService.Data.Entities.LogisticsStatusHistory>();

            ViewBag.CurrentStatusName = (await _workflowServiceClient.FindStatusById(logistics.CurrentStatusId)).Name;

            return View(logistics);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WorkflowAction(int id, [FromForm] string actionName, [FromForm] string? remarks)
        {
            try
            {
                var logistics = await _logisticsRepository.GetByIdAsync(id);
                if (logistics == null)
                    return NotFound(new { success = false, message = $"Logistics record not found (ID: {id})." });

                var userRoles = User.FindAll("appRoleId")
                   .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                   .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                   .Where(id => id.HasValue)
                   .Select(id => id.Value)
                   .ToList();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

                var success = await _workflowService.ExecuteTransition(
                    VMSService.Core.Static.StatusConstants.Entity.Logistics,
                    id,
                    logistics.CurrentStatusId,
                    actionName,
                    userId,
                    userRoles,
                    remarks
                );

                if (!success)
                    return BadRequest(new { success = false, message = "Failed to execute workflow action." });

                // Get updated logistics for new status
                var updatedLogistics = await _logisticsRepository.GetByIdAsync(id);

                StatusTypeDto currentStatus = await _workflowServiceClient.FindStatusById(updatedLogistics.CurrentStatusId);
                return Json(new { success = true, message = "Workflow action completed.", newStatus = currentStatus.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorkflowAction for Logistics ID {Id}, Action: {ActionName}", id, actionName);
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet]
        [Breadcrumb("Edit", FromController = typeof(LogisticsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Edit(int id)
        {
            var logistics = await _logisticsRepository.GetByIdAsync(id);
            if (logistics == null)
                return NotFound();

            StatusTypeDto currentStatus = await _workflowServiceClient.FindStatusById(logistics.CurrentStatusId);

            // Only allow editing if the record is in Draft or Returned status
            if (currentStatus.Name != "Draft" && currentStatus.Name != "Returned")
            {
                TempData["Error"] = "Only draft or returned logistics records can be edited.";
                return RedirectToAction("Details", new { id });
            }

            return View(logistics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] LogisticsCreateViewModel model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid request body." });

            var existingLogistics = await _logisticsRepository.GetByIdAsync(id);
            if (existingLogistics == null)
                return NotFound(new { message = "Logistics record not found." });

            StatusTypeDto currentStatus = await _workflowServiceClient.FindStatusById(existingLogistics.CurrentStatusId);

            // Only allow editing if the record is in Draft or Returned status
            if (currentStatus.Name != "Draft" && currentStatus.Name != "Returned")
            {
                return BadRequest(new { message = "Only draft or returned logistics records can be edited." });
            }

            var logistics = new Data.Entities.Logistics
            {
                Id = id,
                Title = model.Title,
                Description = model.Description,
                ReferenceNumber = model.ReferenceNumber,
                Date = model.Date,
                TeamLeader = model.TeamLeader,
                AssistantTeamLeader = model.AssistantTeamLeader,
                CurrentStatusId = existingLogistics.CurrentStatusId, // Ensure status is preserved
                Categories = model.Categories.Select((cat, catIndex) => new LogisticsCategory
                {
                    Name = cat.Name,
                    SortOrder = catIndex,
                    Details = cat.Details.Select((det, detIndex) => new LogisticsDetail
                    {
                        ItemName = det.ItemName,
                        Description = det.Description,
                        UnitCost = det.UnitCost,
                        Quantity = det.Quantity,
                        Frequency = det.Frequency,
                        TotalCost = det.TotalCost,
                        Notes = det.Notes,
                        SortOrder = detIndex
                    }).ToList()
                }).ToList()
            };

            await _logisticsRepository.UpdateAsync(logistics);

            return Json(new { success = true, message = "Logistics record updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _logisticsRepository.DeleteAsync(id);
            return Json(new { success = true, message = "Logistics record deleted successfully." });
        }
    }

    // View Models
    public class LogisticsCreateViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string? TeamLeader { get; set; }
        public string? AssistantTeamLeader { get; set; }
        public List<LogisticsCategoryViewModel> Categories { get; set; } = new();
    }

    public class LogisticsCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<LogisticsDetailViewModel> Details { get; set; } = new();
    }

    public class LogisticsDetailViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Quantity { get; set; }
        public decimal Frequency { get; set; } = 1;
        public decimal TotalCost { get; set; }
        public string? Notes { get; set; }
    }
}
