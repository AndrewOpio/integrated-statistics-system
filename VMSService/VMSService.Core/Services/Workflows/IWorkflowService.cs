using WorkflowService.Client.Dto;

namespace VMSService.Core.Services.Workflows
{
    public interface IWorkflowService
    {
        /// <summary>
        /// Execute a workflow transition
        /// </summary>
        Task<bool> ExecuteTransition(
            string entityType, int entityId, int currentStatusId,
            string actionName, string username, List<int> userRoles, string remarks, List<long>? attachmentIds = null);

        /// <summary>
        /// Execute a discrepancy transition with stock position update
        /// </summary>
        Task<bool> ExecuteDiscrepancyTransition(int discrepancyId, int currentStatusId, string currentStatusName, int newStatusId, string newStatusName, string userId, string remarks);
        Task<bool> ExecuteTransition(string entityType, Guid entityId, int currentStatusId,string actionName, string username, List<int> userRoles, string remarks, CancellationToken ct, List<long>? attachmentIds = null);
        Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(string entityType, int statusId, int userRole);
    }
}