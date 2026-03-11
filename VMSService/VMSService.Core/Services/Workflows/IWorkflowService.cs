using VMSService.Core.Models.WorkflowTransitions;

namespace VMSService.Core.Services.Workflows
{
    public interface IWorkflowService
    {
        /// <summary>
        /// Get available transitions for current user and status
        /// </summary>
        Task<ICollection<WorkflowTransitionViewModel>> GetAvailableTransitions(string entityType, int currentStatusId, string userRole);
        
        /// <summary>
        /// Execute a workflow transition
        /// </summary>
        Task<bool> ExecuteTransition(string entityType, int entityId, int currentStatusId, string actionName, string userId, List<string> userRoles, string remarks);
    }
}