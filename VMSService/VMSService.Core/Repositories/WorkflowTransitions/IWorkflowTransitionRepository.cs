using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WorkflowTransitions
{
    public interface IWorkflowTransitionRepository : IRepositoryBase<WorkflowTransition, int>
    {
        /// <summary>
        /// Get all available transitions for a specific entity type and current status
        /// </summary>
        Task<ICollection<WorkflowTransition>> FindByEntityTypeAndStatus(string entityType, int currentStatusId);
        
        /// <summary>
        /// Get a specific transition by entity type, from status, and action name
        /// </summary>
        Task<WorkflowTransition> FindByTransitionKey(string entityType, int fromStatusId, string actionName);
    }
}