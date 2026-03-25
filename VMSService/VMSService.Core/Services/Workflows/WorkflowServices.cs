using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Services.Workflows
{
    public class WorkflowServices(
        IApprovedCurrencyProcurementRepository _procurementRepository,
        IApprovedCurrencyStatusHistoryRepository _statusHistoryRepository,
        ILogger<WorkflowServices> _logger,
        IExPrintMintRepository _exPrintMintRepository,
        IReceivedStockRepository _receivedStockRepository,
        IExPrintMintStatusHistoryRepository _exPrintMintStatusHistoryRepository,
        IReceivedStockStatusHistoryRepository _receivedStockStatusHistoryRepository,
        IStockMovementRepository _stockMovementRepository,
        IStockMovementStatusHistoryRepository _stockMovementStatusHistoryRepository,
        IStockMovementDiscrepancyRepository _stockMovementDiscrepancyRepository,
        IWorkflowServiceClient _workflowServiceClient) : IWorkflowService
    {
        //public async Task<ICollection<WorkflowTransitionDto>> GetAvailableTransitions(
        //    string entityType, int currentStatusId, string userRole)
        //{
        //    try
        //    {
        //        // Get all possible transitions from current status
        //        var transitions = await _transitionRepository.FindByEntityTypeAndStatus(entityType, currentStatusId);

        //        // Filter by user role
        //        var availableTransitions = transitions
        //            .Where(t => t.RequiredRole == userRole)
        //            .ToList();

        //        return _mapper.Map<ICollection<WorkflowTransitionViewModel>>(availableTransitions);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting available transitions for {EntityType} status {StatusId} and role {Role}", 
        //            entityType, currentStatusId, userRole);
        //        return new List<WorkflowTransitionViewModel>();
        //    }
        //}

        public async Task<bool> ExecuteTransition(
            string entityType, int entityId, int currentStatusId, 
            string actionName, string username, List<int> userRoles, string remarks)
        {
            var requestData = new GetTransitionByKeyRequestDto
            {
               EntityTypeName = entityType,
               FromStatusId = currentStatusId,
               ActionName = actionName,
            };
            try
            {
                // Find the valid transition
                //var transition = await _transitionRepository.FindByTransitionKey(entityType, currentStatusId, actionName);
                WorkflowTransitionDto transition = await _workflowServiceClient.FindByTransitionKey(requestData);

                if (transition == null)
                {
                    _logger.LogWarning("No valid transition found for {EntityType} {EntityId} action {Action} from status {Status}", 
                        entityType, entityId, actionName, currentStatusId);
                    return false;
                }

                // Verify user role matches transition requirement
                if (!userRoles.Contains(transition.RequiredRole))
                {
                    _logger.LogWarning("User {Username} with role {Role} attempted transition requiring role {RequiredRole}",
                        username, userRoles, transition.RequiredRole);
                    return false;
                }

                // Handle ApprovedCurrencyProcurement transitions
                if (entityType == StatusConstants.Entity.ApprovedCurrencyProcurement)
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
                        OldStatusName = procurement.CurrentStatusName,
                        NewStatusId = transition.ToStatusId,
                        NewStatusName = transition.ToStatus.Name,
                        ChangedBy = username,
                        ChangedAt = DateTime.Now,
                        Remarks = remarks,
                        IsDeleted = false
                    };

                    await _statusHistoryRepository.Create(statusHistory);

                    // Update procurement status
                    procurement.CurrentStatusId = transition.ToStatusId;
                    await _procurementRepository.Update(procurement);

                    _logger.LogInformation("Procurement {Id} transitioned from status {From} to {To} by user {Usrename} with action {Action}",
                        entityId, currentStatusId, transition.ToStatusId, username, actionName);

                    return true;
                }

                // Handle ExPrintMint transitions
                if (entityType == StatusConstants.Entity.ExPrintMint)
                {
                    return await HandleExPrintMintTransitions(entityId, currentStatusId, actionName, username, remarks, transition);
                }

                // Handle ReceivedStock transitions
                if (entityType == StatusConstants.Entity.ReceivedStock)
                {
                    return await HandleReceivedStockTransitions(entityId, currentStatusId, actionName, username, remarks, transition);
                }

                // Handle StockMovement transitions
                if (entityType == StatusConstants.Entity.StockMovement)
                {
                    return await HandleStockMovementTransitions(entityId, currentStatusId, actionName, username, remarks, transition);
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

        private async Task<bool> HandleExPrintMintTransitions(int entityId, int currentStatusId, string actionName,
            string userId, string remarks, WorkflowTransitionDto transition)
        {
            
            var exPrintMint = await _exPrintMintRepository.FindById(entityId);
            if (exPrintMint == null)
            {
                _logger.LogWarning("Expected Stock {Id} not found", entityId);
                return false;
            }

            // Verify current status matches
            if (exPrintMint.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Expected Stock {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, exPrintMint.CurrentStatusId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new ExPrintMintStatusHistory
            {
                ExPrintMintId = entityId,
                OldStatusId = exPrintMint.CurrentStatusId,
                OldStatusName = exPrintMint.CurrentStatusName,
                NewStatusId = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy = userId,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                IsDeleted = false
            };

            await _exPrintMintStatusHistoryRepository.Create(statusHistory);

            // Update procurement status
            exPrintMint.CurrentStatusId = transition.ToStatusId;
            await _exPrintMintRepository.Update(exPrintMint);

            _logger.LogInformation("Expected Stock {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, userId, actionName);

            return true;
        }

        private async Task<bool> HandleReceivedStockTransitions(int entityId, int currentStatusId, string actionName,
            string userId, string remarks, WorkflowTransitionDto transition)
        {

            var receivedStock = await _receivedStockRepository.FindById(entityId);
            if (receivedStock == null)
            {
                _logger.LogWarning("Received Stock {Id} not found", entityId);
                return false;
            }

            // Verify current status matches
            if (receivedStock.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Received Stock {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, receivedStock.CurrentStatusId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new ReceivedStockStatusHistory
            {
                ReceivedStockId = entityId,
                OldStatusId = receivedStock.CurrentStatusId,
                OldStatusName = receivedStock.CurrentStatusName,
                NewStatusId = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy = userId,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                IsDeleted = false
            };

            await _receivedStockStatusHistoryRepository.Create(statusHistory);

            // Update procurement status
            receivedStock.CurrentStatusId = transition.ToStatusId;
            await _receivedStockRepository.Update(receivedStock);

            _logger.LogInformation("Received Stock {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, userId, actionName);

            return true;
        }

        private async Task<bool> HandleStockMovementTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition)
        {

            var stockMovement = await _stockMovementRepository.FindByIdAsync(entityId);
            if (stockMovement == null)
            {
                _logger.LogWarning("Stock Movement {Id} not found", entityId);
                return false;
            }

            // Verify current status matches
            if (stockMovement.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Stock Movement {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, stockMovement.CurrentStatusId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new StockMovementStatusHistory
            {
                StockMovementId = entityId,
                OldStatusId = stockMovement.CurrentStatusId ?? 0,
                OldStatusName = stockMovement.CurrentStatusName,
                NewStatusId = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy = username,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                IsDeleted = false
            };

            await _stockMovementStatusHistoryRepository.Create(statusHistory);

            // Update stock movement status
            stockMovement.CurrentStatusId = transition.ToStatusId;
            await _stockMovementRepository.UpdateAsync(stockMovement);

            _logger.LogInformation("Stock Movement {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        /// <summary>
        /// Handles discrepancy resolution status transitions
        /// </summary>
        public async Task<bool> ExecuteDiscrepancyTransition(
            int discrepancyId, int currentStatusId, string currentStatusName, int newStatusId,
            string newStatusName, string username, string remarks)
        {
            try
            {
                var discrepancy = await _stockMovementDiscrepancyRepository.GetByIdAsync(discrepancyId);

                if (discrepancy == null)
                {
                    _logger.LogWarning("Discrepancy {Id} not found", discrepancyId);
                    return false;
                }

                if (discrepancy.CurrentStatusId != currentStatusId)
                {
                    _logger.LogWarning("Discrepancy {Id} status mismatch", discrepancyId);
                    return false;
                }

                // Create status history record for the stock movement
                var statusHistory = new StockMovementStatusHistory
                {
                    StockMovementId = discrepancy.StockMovementId,
                    OldStatusId = currentStatusId,
                    OldStatusName = currentStatusName,
                    NewStatusId = newStatusId,
                    NewStatusName = newStatusName,
                    ChangedBy = username,
                    ChangedAt = DateTime.Now,
                    Remarks = remarks,
                    IsDeleted = false
                };

                await _stockMovementStatusHistoryRepository.Create(statusHistory);

                // Update discrepancy status
                discrepancy.CurrentStatusId = newStatusId;
                await _stockMovementDiscrepancyRepository.UpdateAsync(discrepancy);

                _logger.LogInformation("Discrepancy {Id} transitioned to status {To} by user {UserId}",
                    discrepancyId, newStatusId, username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing discrepancy transition for Discrepancy {Id}", discrepancyId);
                return false;
            }
        }
    }
}