namespace VMSService.Core.Services.Workflows
{
    public interface IWorkflowService
    {
        /// <summary>
        /// Execute a workflow transition
        /// </summary>
        Task<bool> ExecuteTransition(
            string entityType, int entityId, int currentStatusId,
            string actionName, string username, List<int> userRoles, string remarks);

        /// <summary>
        /// Execute a discrepancy transition with stock position update
        /// </summary>
        Task<bool> ExecuteDiscrepancyTransition(int discrepancyId, int currentStatusId, string currentStatusName, int newStatusId, string newStatusName, string userId, string remarks);
    }
}