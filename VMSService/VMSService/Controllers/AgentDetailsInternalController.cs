using AutoMapper;
using ConfigurationsService.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;
using System.Security.Claims;
using VMSService.Core.Model.AgentDetailStatusHistories;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AgentDetailStatusHistories;
using VMSService.Core.Repositories.CommercialBankUsers;
using VMSService.Core.Services.Configurations;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Controllers
{
    [Authorize]
    public class AgentDetailsInternalController(
        IAgentDetailRepository _agentDetailRepository,
        IConfigurationService _configService,
        IMapper _mapper,
        IWebHostEnvironment _webHostEnvironment,
        ILogger<AgentDetailsController> _logger,
        IHttpContextAccessor _httpContextAccessor,
        ICommercialBankUserRepository _commercialBankUserRepo,
        IWorkflowService _workflowService,
        IWorkflowServiceClient _workflowServiceClient,
        IAgentDetailStatusHistoryRepository _statusHistoryRepository) : Controller
    {
        [Breadcrumb("Agent Details", FromController = typeof(HomeController), FromAction = nameof(Index))]
        // GET: AgentDetails
        public async Task<IActionResult> Index()
        {
            try
            {
                var agentDetails = (await _agentDetailRepository.FindAll()).Where(a => a.IsDeleted == false);
                var model = _mapper.Map<List<AgentDetailViewModel>>(agentDetails);

                // Populate branch names and status names
                var branches = await _configService.GetAllBranches();
                foreach (var agent in model)
                {
                    var branch = branches.FirstOrDefault(b => b.Id == agent.BranchId);
                    agent.BranchName = branch?.Name ?? "Unknown";

                    var statusType = await _workflowServiceClient.FindStatusById(agent.CurrentStatusId);
                    agent.CurrentStatusName = statusType.Name;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent details");
                TempData["Error"] = "An error occurred while retrieving agent details. Please try again.";
                return View(new List<AgentDetailViewModel>());
            }
        }

        // GET: AgentDetailsInternal/Details/5
        [Breadcrumb("Details", FromController = typeof(AgentDetailsInternalController), FromAction = nameof(Index))]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _agentDetailRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id.Value);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<AgentDetailViewModel>(agentDetail);

                // Get available transitions for current user
                //var userRole = User.FindFirst(SystemPolicy.ClaimType.AppRole)?.Value ?? "";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                var availableTransitions = new List<WorkflowTransitionDto>();
                foreach (var userRole in userRoles)
                {
                    var requestData = new GetAvailableTransitionsRequestDto
                    {
                        ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                        EntityTypeName = StatusConstants.Entity.AgentDetail,
                        CurrentStatusId = agentDetail.CurrentStatusId,
                        AppRole = userRole,
                    };

                    var transitions = await _workflowServiceClient.GetAvailableTransitions(requestData);
                    if (transitions != null)
                    {
                        availableTransitions.AddRange(transitions);
                    }
                }

                model.AvailableTransitions = availableTransitions;

                model.StatusHistory = _mapper.Map<ICollection<AgentDetailStatusHistoryViewModel>>(
                    await _statusHistoryRepository.FindByAgentDetailId(id ?? 0));
                foreach (var item in model.StatusHistory)
                {
                    item.OldStatusName = (await _workflowServiceClient.FindStatusById(item.OldStatusId)).Name;
                    item.NewStatusName = (await _workflowServiceClient.FindStatusById(item.NewStatusId)).Name;
                }

                // Populate branch name
                var branches = await _configService.GetAllBranches();
                var branch = branches.FirstOrDefault(b => b.Id == model.BranchId);
                model.BranchName = branch?.Name ?? "Unknown";

                var statusType = await _workflowServiceClient.FindStatusById(model.CurrentStatusId);
                model.CurrentStatusName = statusType.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent detail with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while retrieving the agent detail. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: AgentDetails/Create
        [Breadcrumb("Create", FromController = typeof(AgentDetailsInternalController), FromAction = nameof(Index))]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Get user ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) { return NotFound(); }

                ICollection<CommercialBankBranchDto> commercialBankBranches = [];
                var cbIdClaim = User.Claims.FirstOrDefault(c => c.Type == SystemPolicy.Claims.CbId)?.Value;
                int cbId = string.IsNullOrEmpty(cbIdClaim) ? 0 : int.Parse(cbIdClaim);
                if (cbId > 0)
                {
                    commercialBankBranches = await _configService.GetCommercialBankBranchesByBankId(cbId);
                }

                var branches = await _configService.GetAllBranches();
                var model = new AgentDetailViewModel
                {
                    BranchList = [.. branches.Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = b.Name
                    })],
                    CommercialBankBranchList = [.. commercialBankBranches.Select(cb => new SelectListItem
                    {
                        Value = cb.Id.ToString(),
                        Text = cb.Name
                    })]
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches for create");
                TempData["Error"] = "An error occurred while loading branches. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        } 

        // GET: AgentDetails/Delete/5
        [Breadcrumb("Delete", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                bool exists = await _agentDetailRepository.IsExists(id.Value);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id.Value);
                if (agentDetail == null)
                {
                    return NotFound();
                }

                var model = _mapper.Map<AgentDetailViewModel>(agentDetail);

                // Populate branch name
                var branches = await _configService.GetAllBranches();
                var branch = branches.FirstOrDefault(b => b.Id == model.BranchId);
                model.BranchName = branch?.Name ?? "Unknown";

                var statusType = await _workflowServiceClient.FindStatusById(model.CurrentStatusId);
                model.CurrentStatusName = statusType.Name;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent detail for deletion with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while retrieving the agent detail. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: AgentDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Breadcrumb("Delete", FromController = typeof(AgentDetailsController), FromAction = nameof(Index))]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                bool exists = await _agentDetailRepository.IsExists(id);
                if (!exists)
                {
                    return NotFound();
                }

                var agentDetail = await _agentDetailRepository.FindById(id);
                if (agentDetail != null)
                {
                    agentDetail.IsDeleted = true;
                    bool isDeleted = await _agentDetailRepository.Update(agentDetail);

                    if (isDeleted)
                    {
                        TempData["Success"] = "Agent detail deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to delete agent detail. Please try again.";
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting agent detail with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while deleting the agent detail. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFileAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        // Add generic transition action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAction(int id, string actionName, string remarks)
        {
            try
            {
                var agentDetails = await _agentDetailRepository.FindById(id);
                if (agentDetails == null)
                {
                    TempData["Error"] = "Agent not found.";
                    return RedirectToAction(nameof(Index));
                }

                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                //var userRole = User.FindFirst(SystemPolicy.ClaimType.AppRole)?.Value ?? "";
                var userRoles = User.FindAll("appRoleId")
                    .Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                    .Select(c => int.TryParse(c.Value.Split(":")[1], out var appRoleId) ? appRoleId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                bool isExecuted = await _workflowService.ExecuteTransition(
                    StatusConstants.Entity.AgentDetail,
                    id,
                    agentDetails.CurrentStatusId,
                    actionName,
                    username,
                    userRoles,
                    remarks);

                if (isExecuted)
                {
                    //TempData["Success"] = $"Procurement {actionName} successfully!";
                    TempData["Success"] = $"Review completed successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to execute {actionName} action. You may not have permission or the action is not valid for current status.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action} on procurement {Id}", actionName, id);
                TempData["Error"] = "An error occurred while executing the action.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: AgentDetails/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var agentDetail = await _agentDetailRepository.FindById(id);
                if (agentDetail == null)
                {
                    TempData["Error"] = "Agent not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!agentDetail.IsActive)
                {
                    TempData["Error"] = "Agent is already inactive.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                agentDetail.IsActive = false;

                bool isUpdated = await _agentDetailRepository.Update(agentDetail);
                if (isUpdated)
                {
                    _logger.LogInformation("Agent {AgentId} deactivated by {User}", id,
                        User.FindFirst(ClaimTypes.Name)?.Value ?? "System");
                    TempData["Success"] = "Agent deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to deactivate agent. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating agent with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while deactivating the agent. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: AgentDetails/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var agentDetail = await _agentDetailRepository.FindById(id);
                if (agentDetail == null)
                {
                    TempData["Error"] = "Agent not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (agentDetail.IsActive)
                {
                    TempData["Error"] = "Agent is already active.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                agentDetail.IsActive = true;

                bool isUpdated = await _agentDetailRepository.Update(agentDetail);
                if (isUpdated)
                {
                    _logger.LogInformation("Agent {AgentId} activated by {User}", id,
                        User.FindFirst(ClaimTypes.Name)?.Value ?? "System");
                    TempData["Success"] = "Agent activated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to activate agent. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating agent with id {AgentDetailId}", id);
                TempData["Error"] = "An error occurred while activating the agent. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}