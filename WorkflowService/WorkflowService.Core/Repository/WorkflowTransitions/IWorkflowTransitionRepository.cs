using WorkflowService.Core.Repository.Common;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.WorkflowTransitions
{
    public interface IWorkflowTransitionRepository : IRepositoryBase<WorkflowTransition, int>
    {
        /// <summary>
        /// Get all available transitions for a specific entity type and current status
        /// </summary>
        Task<ICollection<WorkflowTransition>> FindByEntityTypeAndStatus(int entityTypeId, int currentStatusId);

        /// <summary>
        /// Get a specific transition by entity type, from status, and action name
        /// </summary>
        Task<WorkflowTransition> FindByTransitionKey(int entityTypeId, int fromStatusId, string actionName);

        /// <summary>
        /// Get all transitions by applicationId
        /// </summary>
        Task<List<WorkflowTransition>> FindByApplicationId(int applicationId);

        /// <summary>
        /// Get all available transitions for a specific entity type, current status and roleRequired
        /// </summary>
        Task<ICollection<WorkflowTransition>> FindByEntityTypeStatusAndRole(int entityTypeId, int currentStatusId, int requiredRole);

        /// <summary>
        /// Get a map of entity type name → distinct from-status IDs that any of the given roles can act on
        /// </summary>
        Task<Dictionary<string, List<int>>> FindFromStatusesByRoles(int applicationId, IEnumerable<int> roleIds);
    }
}