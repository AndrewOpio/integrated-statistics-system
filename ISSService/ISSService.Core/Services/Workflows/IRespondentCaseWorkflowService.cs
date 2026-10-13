using WorkflowService.Client.Dto;

namespace ISSService.Core.Services.Workflows;

public interface IRespondentCaseWorkflowService
{
    /// <summary>
    /// Resolves the configured initial WorkflowService status
    /// for a new respondent case.
    /// </summary>
    Task<StatusTypeDto> GetInitialStatusAsync(
        string initialStatusName);

    /// <summary>
    /// Returns the workflow actions currently available to a user
    /// for a respondent case.
    /// </summary>
    Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(
        int respondentCaseId,
        List<int> userRoles);

    /// <summary>
    /// Executes an approved WorkflowService transition against
    /// a respondent case and records the resulting ISS status history.
    /// </summary>
    Task<bool> ExecuteTransitionAsync(
        int respondentCaseId,
        string actionName,
        string username,
        List<int> userRoles,
        string? remarks = null);
}