using ISSService.Core.Services.Workflows;
using ISSService.Data.Configurations;
using ISSService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace ISSService.Data.Services.Workflows;

public class RespondentCaseWorkflowService : IRespondentCaseWorkflowService
{
    private const string ApplicationAcronym = "ISS";
    private const string EntityTypeName = "RespondentCase";

    private readonly ISSServiceDbContext _db;
    private readonly IWorkflowServiceClient _workflowServiceClient;

    public RespondentCaseWorkflowService(
        ISSServiceDbContext db,
        IWorkflowServiceClient workflowServiceClient)
    {
        _db = db;
        _workflowServiceClient = workflowServiceClient;
    }

    public async Task<StatusTypeDto> GetInitialStatusAsync(
        string initialStatusName)
    {
        if (string.IsNullOrWhiteSpace(initialStatusName))
        {
            throw new ArgumentException(
                "The initial respondent case workflow status is required.",
                nameof(initialStatusName));
        }

        var request = new GetStatusByNameRequestDto
        {
            ApplicationAcronym = ApplicationAcronym,
            EntityTypeName = EntityTypeName,
            StatusName = initialStatusName.Trim()
        };

        var status =
            await _workflowServiceClient.FindStatusByName(request);

        if (status == null ||
            status.Id <= 0 ||
            string.IsNullOrWhiteSpace(status.Name))
        {
            throw new InvalidOperationException(
                $"WorkflowService could not resolve the initial status " +
                $"'{initialStatusName}' for application " +
                $"'{ApplicationAcronym}' and entity type " +
                $"'{EntityTypeName}'. Ensure the workflow is configured " +
                $"before creating respondent cases.");
        }

        return status;
    }

    public async Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(
        int respondentCaseId,
        List<int> userRoles)
    {
        var respondentCase = await _db.RespondentCases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == respondentCaseId);

        if (respondentCase == null)
        {
            throw new InvalidOperationException(
                $"Respondent case {respondentCaseId} was not found.");
        }

        if (respondentCase.CurrentStatusId <= 0)
        {
            throw new InvalidOperationException(
                $"Respondent case {respondentCaseId} does not have " +
                "a valid WorkflowService status.");
        }

        if (userRoles == null || userRoles.Count == 0)
        {
            return new List<WorkflowTransitionDto>();
        }

        var transitions = new List<WorkflowTransitionDto>();

        foreach (var roleId in userRoles
                     .Where(x => x > 0)
                     .Distinct())
        {
            var roleTransitions =
                await _workflowServiceClient.GetAvailableTransitionsAsync(
                    ApplicationAcronym,
                    EntityTypeName,
                    respondentCase.CurrentStatusId,
                    roleId);

            if (roleTransitions == null)
            {
                continue;
            }

            transitions.AddRange(
                roleTransitions.Where(IsValidTransition));
        }

        return transitions
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();
    }

    public async Task<bool> ExecuteTransitionAsync(
        int respondentCaseId,
        string actionName,
        string username,
        List<int> userRoles,
        string? remarks = null)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException(
                "Workflow action is required.",
                nameof(actionName));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Username is required.",
                nameof(username));
        }

        if (userRoles == null || userRoles.Count == 0)
        {
            throw new InvalidOperationException(
                "The user does not have any workflow roles.");
        }

        var respondentCase = await _db.RespondentCases
            .FirstOrDefaultAsync(x => x.Id == respondentCaseId);

        if (respondentCase == null)
        {
            throw new InvalidOperationException(
                $"Respondent case {respondentCaseId} was not found.");
        }

        if (respondentCase.CurrentStatusId <= 0)
        {
            throw new InvalidOperationException(
                $"Respondent case {respondentCaseId} does not have " +
                "a valid WorkflowService status.");
        }

        var request = new GetTransitionByKeyRequestDto
        {
            EntityTypeName = EntityTypeName,
            FromStatusId = respondentCase.CurrentStatusId,
            ActionName = actionName.Trim()
        };

        var transition =
            await _workflowServiceClient.FindByTransitionKey(request);

        if (!IsValidTransition(transition))
        {
            throw new InvalidOperationException(
                $"No valid WorkflowService transition is configured " +
                $"for action '{actionName}' from status " +
                $"'{respondentCase.CurrentStatusName}' " +
                $"({respondentCase.CurrentStatusId}).");
        }

        if (!userRoles.Contains(transition.RequiredRole))
        {
            throw new UnauthorizedAccessException(
                $"The user is not authorised to perform the " +
                $"'{actionName}' workflow action.");
        }

        if (transition.RequiresComments &&
            string.IsNullOrWhiteSpace(remarks))
        {
            throw new InvalidOperationException(
                $"The '{actionName}' workflow action requires remarks.");
        }

        var destinationStatus =
            transition.ToStatus;

        if (destinationStatus == null ||
            destinationStatus.Id <= 0 ||
            string.IsNullOrWhiteSpace(destinationStatus.Name))
        {
            destinationStatus =
                await _workflowServiceClient.FindStatusById(
                    transition.ToStatusId);
        }

        if (destinationStatus == null ||
            destinationStatus.Id <= 0 ||
            string.IsNullOrWhiteSpace(destinationStatus.Name))
        {
            throw new InvalidOperationException(
                $"WorkflowService returned transition " +
                $"{transition.Id}, but destination status " +
                $"{transition.ToStatusId} could not be resolved.");
        }

        var now = DateTime.UtcNow;

        var oldStatusId = respondentCase.CurrentStatusId;
        var oldStatusName = respondentCase.CurrentStatusName;

        respondentCase.CurrentStatusId =
            destinationStatus.Id;

        respondentCase.CurrentStatusName =
            destinationStatus.Name;

        respondentCase.ModifiedDate = now;
        respondentCase.ModifiedBy = username;

        var history = new RespondentCaseStatusHistory
        {
            RespondentCaseId = respondentCase.Id,
            WorkflowTransitionId = transition.Id,

            OldStatusId = oldStatusId,
            OldStatusName = oldStatusName,

            NewStatusId = destinationStatus.Id,
            NewStatusName = destinationStatus.Name,

            ActionName = actionName.Trim(),
            Remarks = string.IsNullOrWhiteSpace(remarks)
                ? null
                : remarks.Trim(),

            ChangedDate = now,
            ChangedBy = username,

            CreatedDate = now,
            CreatedBy = username
        };

        _db.RespondentCaseStatusHistories.Add(history);

        await _db.SaveChangesAsync();

        return true;
    }

    private static bool IsValidTransition(
        WorkflowTransitionDto? transition)
    {
        return transition != null
            && transition.Id > 0
            && transition.EntityTypeId > 0
            && transition.FromStatusId > 0
            && transition.ToStatusId > 0
            && transition.RequiredRole > 0
            && !string.IsNullOrWhiteSpace(transition.ActionName)
            && transition.IsActive
            && !transition.IsDeleted;
    }
}