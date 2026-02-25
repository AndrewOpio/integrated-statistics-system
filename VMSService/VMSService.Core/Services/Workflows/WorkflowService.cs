using AutoMapper;
using Microsoft.Extensions.Logging;
using VMSService.Core.Models.WorkflowTransitions;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.WorkflowTransitions;
using VMSService.Core.Static;
using VMSService.Data.Entities;

namespace VMSService.Core.Services.Workflows
{
    public class WorkflowService(
        IWorkflowTransitionRepository _transitionRepository,
        IApprovedCurrencyProcurementRepository _procurementRepository,
        IApprovedCurrencyStatusHistoryRepository _statusHistoryRepository,
        IMapper _mapper,
        ILogger<WorkflowService> _logger) : IWorkflowService
    {
        public async Task<ICollection<WorkflowTransitionViewModel>> GetAvailableTransitions(
            string entityType, int currentStatusId, string userRole)
        {
            try
            {
                // Get all possible transitions from current status
                var transitions = await _transitionRepository.FindByEntityTypeAndStatus(entityType, currentStatusId);

                // Filter by user role
                var availableTransitions = transitions
                    .Where(t => t.RequiredRole == userRole)
                    .ToList();

                return _mapper.Map<ICollection<WorkflowTransitionViewModel>>(availableTransitions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available transitions for {EntityType} status {StatusId} and role {Role}", 
                    entityType, currentStatusId, userRole);
                return new List<WorkflowTransitionViewModel>();
            }
        }

        public async Task<bool> ExecuteTransition(
            string entityType, int entityId, int currentStatusId, 
            string actionName, string userId, string userRole, string remarks)
        {
            try
            {
                // Find the valid transition
                var transition = await _transitionRepository.FindByTransitionKey(entityType, currentStatusId, actionName);

                if (transition == null)
                {
                    _logger.LogWarning("No valid transition found for {EntityType} {EntityId} action {Action} from status {Status}", 
                        entityType, entityId, actionName, currentStatusId);
                    return false;
                }

                // Verify user role matches transition requirement
                if (transition.RequiredRole != userRole)
                {
                    _logger.LogWarning("User {UserId} with role {Role} attempted transition requiring role {RequiredRole}", 
                        userId, userRole, transition.RequiredRole);
                    return false;
                }

                // Handle ApprovedCurrencyProcurement transitions
                if (entityType == "ApprovedCurrencyProcurement")
                {
                    var procurement = await _procurementRepository.FindById(entityId);
                    if (procurement == null)
                    {
                        _logger.LogWarning("Procurement {Id} not found", entityId);
                        return false;
                    }

                    // Verify current status matches
                    if (procurement.CurrentStatusId != currentStatusId)
                    {
                        _logger.LogWarning("Procurement {Id} status mismatch. Current: {Current}, Expected: {Expected}", 
                            entityId, procurement.CurrentStatusId, currentStatusId);
                        return false;
                    }

                    // Create status history record
                    var statusHistory = new ApprovedCurrencyStatusHistory
                    {
                        ApprovedCurrencyProcurementId = entityId,
                        OldStatusId = procurement.CurrentStatusId,
                        NewStatusId = transition.ToStatusId,
                        ChangedBy = userId,
                        ChangedAt = DateTime.UtcNow,
                        Remarks = remarks,
                        IsDeleted = false
                    };

                    await _statusHistoryRepository.Create(statusHistory);

                    // Update procurement status
                    procurement.CurrentStatusId = transition.ToStatusId;
                    await _procurementRepository.Update(procurement);

                    _logger.LogInformation("Procurement {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}",
                        entityId, currentStatusId, transition.ToStatusId, userId, actionName);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transition for {EntityType} {EntityId} action {Action}", 
                    entityType, entityId, actionName);
                return false;
            }
        }
    }
}