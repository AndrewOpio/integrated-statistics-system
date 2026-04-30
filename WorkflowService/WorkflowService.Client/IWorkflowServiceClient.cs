using WorkflowService.Client.Dto;

namespace WorkflowService.Client
{
    public interface IWorkflowServiceClient
    {
        /// <summary>
        /// Get available transitions for current user and status
        /// </summary>
        Task<List<WorkflowTransitionDto>> GetAvailableTransitions(GetAvailableTransitionsRequestDto requestData);

        /// <summary>
        /// Get transition for entity, current status and action
        /// </summary>
        Task<WorkflowTransitionDto> FindByTransitionKey(GetTransitionByKeyRequestDto requestData);

        Task<List<StatusTypeDto>> FindAllStatuses();
        Task<StatusTypeDto> FindStatusByName(GetStatusByNameRequestDto requestData);
        Task<StatusTypeDto> FindStatusById(int statusId);
        Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(string entityType, int statusId, int userRole);
    }
}
