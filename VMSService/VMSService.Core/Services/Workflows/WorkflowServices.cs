using Microsoft.Extensions.Logging;
using NotificationService.Client;
using NotificationService.Client.Dtos;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AgentDetailStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories;
using VMSService.Core.Repositories.EodDiscrepancyReports;
using VMSService.Core.Repositories.MonthlySurpluses;
using VMSService.Core.Repositories.MonthlySurplusStatusHistories;
using VMSService.Core.Repositories.MonthlyShortages;
using VMSService.Core.Repositories.MonthlyShortageStatusHistories;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizationStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Repositories.DestructionRequestStatusHistories;
using VMSService.Core.Repositories.DestructionRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Repositories.StockProcessingPlanStatusHistories;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Submissions;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Services.Workflows
{
    public class WorkflowServices(
        IApprovedCurrencyProcurementRepository _procurementRepository,
        IApprovedCurrencyStatusHistoryRepository _statusHistoryRepository,
        ILogger<WorkflowServices> _logger,
        IExPrintMintRepository _exPrintMintRepository,
        IReceivedStockRepository _receivedStockRepository,
        IExPrintMintStatusHistoryRepository _exPrintMintStatusHistoryRepository,
        IStockProcessingPlanRepository _stockProcessingPlanRepository,
        IStockProcessingPlanStatusHistoryRepository _stockProcessingPlanStatusHistoryRepository,
        IReceivedStockStatusHistoryRepository _receivedStockStatusHistoryRepository,
        IStockMovementRepository _stockMovementRepository,
        IStockMovementStatusHistoryRepository _stockMovementStatusHistoryRepository,
        IStockMovementDiscrepancyRepository _stockMovementDiscrepancyRepository,
        IWorkflowServiceClient _workflowServiceClient,
        INotificationServiceClient _notificationServiceClient,
        IIdentityService _identityService,
        IWithdrawalTransactionRefNumberGenerator _txnrefNoGenerator,
        IWithdrawalUnitOfWork _uow,
        IAgentDetailRepository _agentDetailRepository,
        IAgentDetailStatusHistoryRepository _agentDetailStatusHistoryRepository,
        IDepositUnitOfWork _duow,
        IDestructionRequestRepository _destructionRequestRepository,
        IDestructionRequestStatusHistoryRepository _destructionStatusHistoryRepository,
        IDestructionAuthorizationRepository _destructionAuthorizationRepository,
        IDestructionAuthorizationStatusHistoryRepository _destructionAuthorizationStatusHistoryRepository,
        IEodDiscrepancyReportRepository _cfbReportRepository,
        IEodDiscrepancyReportStatusHistoryRepository _cfbReportStatusHistoryRepository,
        IMonthlySurplusRepository _monthlySurplusRepository,
        IMonthlySurplusStatusHistoryRepository _monthlySurplusStatusHistoryRepository,
        IMonthlyShortageRepository _monthlyShortageRepository,
        IMonthlyShortageStatusHistoryRepository _monthlyShortageStatusHistoryRepository,
        IMonthlyCommercialBankSettlementRepository _cbSettlementRepository,
        IMonthlyCommercialBankSettlementStatusHistoryRepository _cbSettlementStatusHistoryRepository) : IWorkflowService
    {


        public async Task<bool> ExecuteTransition(
            string entityType, int entityId, int currentStatusId,
            string actionName, string username, List<int> userRoles, string remarks,
            List<long>? attachmentIds = null)
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

                switch (entityType)
                {
                    case StatusConstants.Entity.ApprovedCurrencyProcurement:
                        return await HandleApprovedCurrencyProcurementTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.ExPrintMint:
                        return await HandleExPrintMintTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.ReceivedStock:
                        return await HandleReceivedStockTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.StockMovement:
                        return await HandleStockMovementTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.AgentDetail:
                        return await HandleAgentDetailTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.DestructionRequest:
                        return await HandleDestructionRequestTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.DestructionAuthorization:
                        return await HandleDestructionAuthorizationTransitions(entityId, currentStatusId, actionName, username, remarks, transition);

                    case StatusConstants.Entity.CfbReport:
                        return await HandleCfbReportTransitions(entityId, currentStatusId, actionName, username, remarks, transition);

                    case StatusConstants.Entity.MonthlySurpluses:
                        return await HandleMonthlySurplusTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.MonthlyShortages:
                        return await HandleMonthlyShortageTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    case StatusConstants.Entity.MonthlyCommercialBankSettlements:
                        return await HandleMonthlyCommercialBankSettlementTransitions(entityId, currentStatusId, actionName, username, remarks, transition, attachmentIds);

                    default:
                        _logger.LogWarning("Unknown entity type: {EntityType}", entityType);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transition for {EntityType} {EntityId} action {Action}",
                    entityType, entityId, actionName);
                return false;
            }
        }
        public async Task<bool> ExecuteTransition(
            string entityType, 
            Guid entityId, 
            int currentStatusId, 
            string actionName, 
            string username, 
            List<int> userRoles, 
            string remarks, 
            CancellationToken ct,
            List<long>? attachmentIds = null)
        {
            var requestData = new GetTransitionByKeyRequestDto
            {
                EntityTypeName = entityType,
                FromStatusId = currentStatusId,
                ActionName = actionName,
            };

            try
            {
                WorkflowTransitionDto transition = await _workflowServiceClient
                    .FindByTransitionKey(requestData);

                if (transition == null)
                {
                    _logger.LogWarning(
                        "No valid transition found for {EntityType} {EntityId} action {Action} from status {Status}",
                        entityType, entityId, actionName, currentStatusId);
                    return false;
                }

                if (!userRoles.Contains(transition.RequiredRole))
                {
                    _logger.LogWarning(
                        "User {Username} with roles {Roles} attempted transition requiring role {RequiredRole}",
                        username, userRoles, transition.RequiredRole);
                    return false;
                }

                return entityType switch
                {
                    StatusConstants.Entity.WithdrawalRequest =>
                        await HandleWithdrawalTransitions(
                            entityId, currentStatusId, actionName,
                            username, remarks, transition),

                    StatusConstants.Entity.DepositRequest =>
                            await HandleDepositTransitions(
                            entityId, currentStatusId, actionName,
                            username, remarks, transition, ct, attachmentIds), // Pass attachmentIds

                    StatusConstants.Entity.DepositRequestExceptional =>
                            await HandleDepositTransitions(
                            entityId, currentStatusId, actionName,
                            username, remarks, transition, ct, attachmentIds), // Pass attachmentIds

                    _ => LogUnknownEntity(entityType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error executing transition for {EntityType} {EntityId} action {Action}",
                    entityType, entityId, actionName);
                return false;
            }
        }

        private bool LogUnknownEntity(string entityType)
        {
            _logger.LogWarning("Unknown entity type: {EntityType}", entityType);
            return false;
        }

        private async Task<bool> HandleApprovedCurrencyProcurementTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
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
                AttachmentIds = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _statusHistoryRepository.Create(statusHistory);

            // Update procurement status
            procurement.CurrentStatusId = transition.ToStatusId;
            procurement.CurrentStatusName = transition.ToStatus.Name;
            await _procurementRepository.Update(procurement);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Approved Currency Procurement #{entityId} (UGX {procurement.Amount.ToString("N0")})",
                transition,
                statusHistory.NewStatusName,
                username, remarks);
            }

            _logger.LogInformation("Approved Currency Procurement {Id} transitioned from status {From} to {To} by user {Username} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        private async Task<bool> HandleExPrintMintTransitions(int entityId, int currentStatusId, string actionName,
            string userId, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
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
                AttachmentIds = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _exPrintMintStatusHistoryRepository.Create(statusHistory);

            // Update procurement status
            exPrintMint.CurrentStatusId = transition.ToStatusId;
            exPrintMint.CurrentStatusName = transition.ToStatus.Name;
            await _exPrintMintRepository.Update(exPrintMint);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Expected Stock Reference {exPrintMint.ExPrintMintReference}",
                transition,
                statusHistory.NewStatusName,
                userId, remarks);
            }

            _logger.LogInformation("Expected Stock {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, userId, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        private async Task<bool> HandleReceivedStockTransitions(int entityId, int currentStatusId, string actionName,
            string userId, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
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
                AttachmentIds = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _receivedStockStatusHistoryRepository.Create(statusHistory);

            // Update procurement status
            receivedStock.CurrentStatusId = transition.ToStatusId;
            receivedStock.CurrentStatusName = transition.ToStatus.Name;
            await _receivedStockRepository.Update(receivedStock);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Received Stock Reference {receivedStock.ExPrintMint.ExPrintMintReference}",
                transition,
                statusHistory.NewStatusName,
                userId,
                remarks);
            }

            _logger.LogInformation("Received Stock {Id} transitioned from status {From} to {To} by user {UserId} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, userId, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        private async Task<bool> HandleStockMovementTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
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
                AttachmentIds = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _stockMovementStatusHistoryRepository.Create(statusHistory);

            // Update stock movement status
            stockMovement.CurrentStatusId = transition.ToStatusId;
            stockMovement.CurrentStatusName = transition.ToStatus.Name;
            await _stockMovementRepository.UpdateAsync(stockMovement);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Stock Movement {entityId}",
                transition,
                statusHistory.NewStatusName,
                username,
                remarks);
            }

            _logger.LogInformation("Stock Movement {Id} transitioned from status {From} to {To} by user {Username} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        /// <summary>
        /// Sends email notifications to specified recipients and users with specified roles.
        /// Parses comma-separated NotificationEmails and NotificationRoleIds from the transition.
        /// </summary>
        private async Task SendTransitionNotification(string entityDescription, WorkflowTransitionDto transition,
            string newStatusName, string changedByUser, string remarks)
        {
            try
            {
                ICollection<RecipientDto> recipients = await _identityService.GetRecipients(transition.NotificationEmails, transition.NotificationRoleIds);

                // Only send if there are recipients
                if (!recipients.Any())
                {
                    _logger.LogWarning("No recipients found for notification. Transition for {Entity}", entityDescription);
                    return;
                }

                var notificationTitle = $"{entityDescription} {newStatusName}";
                var notificationBody = $@"
                    <p>Hello,</p>
                    <p>The {entityDescription} is {newStatusName}:</p>
                    <ul>
                        <li><strong>Transitioned By:</strong> {changedByUser}</li>
                        <li><strong>Comment:</strong> {remarks}</li>
                        <li><strong>Transition Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</li>
                    </ul>
                    <p>Please log in to the CAS system for more details.</p>
                    <p>Thank you.</p>
                    <small>THIS IS AN AUTOMATED MESSAGE. PLEASE DO NOT REPLY.</small>";

                var notificationRequest = new SendNotificationRequestDto
                {
                    Title = notificationTitle,
                    Body = notificationBody,
                    NotificationTypeId = 1, // 1 = Email
                    Recipients = recipients.ToList(),
                };

                var notificationId = await _notificationServiceClient.SendAsync(notificationRequest);

                _logger.LogInformation("Notification sent successfully. NotificationId: {NotificationId} for entity {Entity}",
                    notificationId, entityDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transition notification for {Entity}", entityDescription);
                // Don't throw - notifications are supplementary, not critical
            }
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
                discrepancy.CurrentStatusName = newStatusName;
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

        private async Task<bool> HandleWithdrawalTransitions(Guid entityId, int currentStatusId, string actionName, string username, string remarks, WorkflowTransitionDto transition)
        {
            var request = await _uow.Requests.FindById(entityId);
            if (request is null)
            {
                _logger.LogWarning("WithdrawalRequest {Id} not found", entityId);
                return false;
            }

            // Verify current status matches � prevents race conditions
            if (request.StatusTypeId != currentStatusId)
            {
                _logger.LogWarning(
                    "WithdrawalRequest {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, request.StatusTypeId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new WithdrawalRequestStatusHistory
            {
                WithdrawalRequestId = entityId,
                OldStatusId = request.StatusTypeId,
                OldStatusName = request.StatusTypeName,
                NewStatusId = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy = username,
                ChangedAt = DateTime.UtcNow,
                Remarks = remarks,
                CreatedBy = username,
                CreatedDate = DateTime.Now,
            };
            await _uow.StatusHistory.Create(statusHistory);

            // Update request status
            request.StatusTypeId = transition.ToStatusId;
            request.StatusTypeName = transition.ToStatus.Name;
            request.ModifiedBy = username;
            request.ModifiedDate = DateTime.Now;

            // CB approval is the submission event — classify same-day submissions here
            if (transition.ToStatus.Name == WithdrawalStatusNames.PendingRTGSVerification)
            {
                // TODO: isDayBeforeHoliday is simplified (always false). See SubmissionClassifier
                // for the full TODO — a future implementation will look up the holiday calendar.
                var classification = SubmissionClassifier.Classify(
                    statusHistory.ChangedAt, request.CollectionDate, isDayBeforeHoliday: false);

                if (classification == SubmissionClassification.LateInvalid)
                {
                    _logger.LogWarning(
                        "WithdrawalRequest {Id} approval rejected: approval date is past collection date {CollectionDate}",
                        entityId, request.CollectionDate);
                    return false;
                }

                // Only derive from classification if not already explicitly set via SetSameDayReason.
                if (!request.RequiresSpecialApproval)
                {
                    request.RequiresSpecialApproval = classification == SubmissionClassification.SameDayExceptional;
                    request.SpecialApprovalReason = classification == SubmissionClassification.SameDayExceptional
                        ? remarks
                        : null;
                }
            }

            // If supervisor is submitting to bank � generate ref number + copy denominations
            //if (transition.ToStatusId == WithdrawStatuses.WithdrawalStatusIds.SubmittedToBOU)
            //{
            //    //request.SupervisorUserId = username;

            //    request.TransactionRefNumber = await _txnrefNoGenerator.GenerateAsync(
            //        bouBranchId: request.BouBranchId,
            //        cbId: request.CbId,
            //        collectionDate: request.CollectionDate,
            //        ct: CancellationToken.None);

            //    await _uow.Denominations.CopyToBouAsync(request.Id, username, CancellationToken.None); ;
            //}

            await _uow.Requests.Update(request);

            // Send notification if configured in WorkflowService
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Withdrawal Request #{request.TransactionRefNumber ?? entityId.ToString()} " +
                    $"(UGX {request.TotalAmount:N0})",
                    transition,
                    statusHistory.NewStatusName,
                    username,
                    remarks);
            }

            await _uow.SaveAsync(CancellationToken.None);

            _logger.LogInformation(
                "WithdrawalRequest {Id} transitioned from {From} to {To} by {Username} via action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleDepositTransitions(Guid entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, CancellationToken ct,
            List<long>? attachmentIds = null) // Add this parameter
        {
            var deposit = await _duow.Requests.FindById(entityId);
            if (deposit == null)
            {
                _logger.LogWarning("Deposit {Id} not found", entityId);
                return false;
            }

            // Verify current status matches
            if (deposit.StatusTypeId != currentStatusId)
            {
                _logger.LogWarning("Deposit {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, deposit.StatusTypeId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new DepositRequestStatusHistory
            {
                DepositRequestId = entityId,
                OldStatusId = deposit.StatusTypeId,
                NewStatusId = transition.ToStatusId,
                ChangedBy = username,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                AttachmentIds = attachmentIds != null && attachmentIds.Any() 
                    ? string.Join(",", attachmentIds) 
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _duow.StatusHistory.Create(statusHistory);

            // Update deposit status
            deposit.StatusTypeId = transition.ToStatusId;
            await _duow.Requests.Update(deposit);

            await _duow.SaveAsync(ct);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Deposit {deposit.TransactionRefNumber} (UGX {deposit.TotalAmount:N0})",
                transition,
                transition.ToStatus.Name,
                username, remarks);
            }

            _logger.LogInformation(
                "Deposit {Id} transitioned from status {From} to {To} by user {Username} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        public async Task<List<WorkflowTransitionDto>> GetAvailableTransitionsAsync(string entityType, int statusId, int userRole)
        {
            try
            {
                var requestData = new GetAvailableTransitionsRequestDto
                {
                    ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                    EntityTypeName = entityType,
                    CurrentStatusId = statusId,
                    AppRole = userRole,
                };

                return await _workflowServiceClient.GetAvailableTransitions(requestData)
                       ?? new List<WorkflowTransitionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching available transitions for {EntityType} status {StatusId}",
                    entityType, statusId);
                return new List<WorkflowTransitionDto>();
            }
        }

        private async Task<bool> HandleStockProcessingPlanTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var plan = await _stockProcessingPlanRepository.FindById(entityId);
            if (plan == null)
            {
                _logger.LogWarning("Stock Processing Plan {Id} not found", entityId);
                return false;
            }

            if (plan.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Stock Processing Plan {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, plan.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new StockProcessingPlanStatusHistory
            {
                StockProcessingPlanId = entityId,
                OldStatusId = plan.CurrentStatusId,
                NewStatusId = transition.ToStatusId,
                ChangedBy = username,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                Attachments = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null,
                IsDeleted = false
            };

            await _stockProcessingPlanStatusHistoryRepository.Create(statusHistory);

            plan.CurrentStatusId = transition.ToStatusId;
            await _stockProcessingPlanRepository.Update(plan);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Stock Processing Plan #{entityId}",
                    transition,
                    transition.ToStatus.Name,
                    username, remarks);
            }

            _logger.LogInformation("Stock Processing Plan {Id} transitioned from status {From} to {To} by user {Username} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        private async Task<bool> HandleAgentDetailTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var agentDetail = await _agentDetailRepository.FindById(entityId);
            if (agentDetail == null)
            {
                _logger.LogWarning("Agent Detail {Id} not found", entityId);
                return false;
            }

            // Verify current status matches
            if (agentDetail.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Agent Detail {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, agentDetail.CurrentStatusId, currentStatusId);
                return false;
            }

            // Create status history record
            var statusHistory = new AgentDetailStatusHistory
            {
                AgentDetailId = entityId,
                OldStatusId = agentDetail.CurrentStatusId,
                NewStatusId = transition.ToStatusId,
                ChangedBy = username,
                ChangedAt = DateTime.Now,
                Remarks = remarks,
                AttachmentIds = attachmentIds != null && attachmentIds.Any()
                    ? string.Join(",", attachmentIds)
                    : null, // Store comma-separated attachment IDs
                IsDeleted = false
            };

            await _agentDetailStatusHistoryRepository.Create(statusHistory);

            // Update agent detail status
            agentDetail.CurrentStatusId = transition.ToStatusId;
            await _agentDetailRepository.Update(agentDetail);

            // Send notifications
            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                $"Agent {agentDetail.AgentNames}",
                transition,
                transition.ToStatus.Name,
                username, remarks);
            }

            _logger.LogInformation("Agent Detail {Id} transitioned from status {From} to {To} by user {Username} with action {Action}. Attachments: {AttachmentCount}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName, attachmentIds?.Count ?? 0);

            return true;
        }

        private async Task<bool> HandleDestructionRequestTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var request = await _destructionRequestRepository.FindById(entityId);
            if (request == null)
            {
                _logger.LogWarning("Destruction Request {Id} not found", entityId);
                return false;
            }

            if (request.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Destruction Request {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, request.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new DestructionRequestStatusHistory
            {
                DestructionRequestId = entityId,
                OldStatusId          = request.CurrentStatusId,
                OldStatusName        = request.CurrentStatusName,
                NewStatusId          = transition.ToStatusId,
                NewStatusName        = transition.ToStatus.Name,
                ChangedBy            = username,
                ChangedAt            = DateTime.Now,
                Remarks              = remarks,
                AttachmentIds        = attachmentIds != null && attachmentIds.Any()
                                           ? string.Join(",", attachmentIds)
                                           : null,
                IsDeleted            = false
            };

            await _destructionStatusHistoryRepository.Create(statusHistory);

            request.CurrentStatusId = transition.ToStatusId;
            request.CurrentStatusName = transition.ToStatus.Name;
            await _destructionRequestRepository.Update(request);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Destruction Request {request.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("Destruction Request {Id} transitioned from status {From} to {To} by user {Username} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleCfbReportTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition)
        {
            var report = await _cfbReportRepository.FindById(entityId);
            if (report == null)
            {
                _logger.LogWarning("CFB Report {Id} not found", entityId);
                return false;
            }

            if (report.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("CFB Report {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, report.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new EodDiscrepancyReportStatusHistory
            {
                EodDiscrepancyReportId = entityId,
                OldStatusId   = report.CurrentStatusId,
                OldStatusName = report.CurrentStatusName,
                NewStatusId   = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy     = username,
                ChangedAt     = DateTime.Now,
                Remarks       = remarks,
                IsDeleted     = false
            };

            await _cfbReportStatusHistoryRepository.Create(statusHistory);

            report.CurrentStatusId   = transition.ToStatusId;
            report.CurrentStatusName = transition.ToStatus.Name;
            await _cfbReportRepository.Update(report);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"CFB Report {report.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("CFB Report {Id} transitioned from status {From} to {To} by user {Username} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleMonthlySurplusTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var settlement = await _monthlySurplusRepository.FindById(entityId);
            if (settlement == null)
            {
                _logger.LogWarning("Monthly Settlement {Id} not found", entityId);
                return false;
            }

            if (settlement.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Monthly Settlement {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, settlement.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new MonthlySurplusStatusHistory
            {
                MonthlySurplusId = entityId,
                OldStatusId         = settlement.CurrentStatusId,
                OldStatusName       = settlement.CurrentStatusName,
                NewStatusId         = transition.ToStatusId,
                NewStatusName       = transition.ToStatus.Name,
                ChangedBy           = username,
                ChangedAt           = DateTime.Now,
                Remarks             = remarks,
                AttachmentIds       = attachmentIds != null && attachmentIds.Any()
                                          ? string.Join(",", attachmentIds)
                                          : null,
                IsDeleted           = false
            };

            await _monthlySurplusStatusHistoryRepository.Create(statusHistory);

            settlement.CurrentStatusId   = transition.ToStatusId;
            settlement.CurrentStatusName = transition.ToStatus.Name;
            await _monthlySurplusRepository.Update(settlement);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Monthly Settlement {settlement.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("Monthly Settlement {Id} transitioned from {From} to {To} by {Username} via action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleMonthlyShortageTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var shortage = await _monthlyShortageRepository.FindById(entityId);
            if (shortage == null)
            {
                _logger.LogWarning("Monthly Shortage {Id} not found", entityId);
                return false;
            }

            if (shortage.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Monthly Shortage {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, shortage.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new MonthlyShortageStatusHistory
            {
                MonthlyShortageId = entityId,
                OldStatusId       = shortage.CurrentStatusId,
                OldStatusName     = shortage.CurrentStatusName,
                NewStatusId       = transition.ToStatusId,
                NewStatusName     = transition.ToStatus.Name,
                ChangedBy         = username,
                ChangedAt         = DateTime.Now,
                Remarks           = remarks,
                AttachmentIds     = attachmentIds != null && attachmentIds.Count > 0
                                        ? string.Join(",", attachmentIds)
                                        : null,
                IsDeleted         = false
            };

            await _monthlyShortageStatusHistoryRepository.Create(statusHistory);

            shortage.CurrentStatusId   = transition.ToStatusId;
            shortage.CurrentStatusName = transition.ToStatus.Name;
            await _monthlyShortageRepository.Update(shortage);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Monthly Shortage {shortage.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("Monthly Shortage {Id} transitioned from {From} to {To} by {Username} via action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleMonthlyCommercialBankSettlementTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition, List<long>? attachmentIds = null)
        {
            var settlement = await _cbSettlementRepository.FindById(entityId);
            if (settlement == null)
            {
                _logger.LogWarning("CB Settlement {Id} not found", entityId);
                return false;
            }

            if (settlement.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("CB Settlement {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, settlement.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new MonthlyCommercialBankSettlementStatusHistory
            {
                MonthlyCommercialBankSettlementId = entityId,
                OldStatusId   = settlement.CurrentStatusId,
                OldStatusName = settlement.CurrentStatusName,
                NewStatusId   = transition.ToStatusId,
                NewStatusName = transition.ToStatus.Name,
                ChangedBy     = username,
                ChangedAt     = DateTime.Now,
                Remarks       = remarks,
                AttachmentIds = attachmentIds != null && attachmentIds.Count > 0
                                    ? string.Join(",", attachmentIds)
                                    : null,
                IsDeleted     = false
            };

            await _cbSettlementStatusHistoryRepository.Create(statusHistory);

            settlement.CurrentStatusId   = transition.ToStatusId;
            settlement.CurrentStatusName = transition.ToStatus.Name;
            await _cbSettlementRepository.Update(settlement);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"CB Settlement {settlement.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("CB Settlement {Id} transitioned from {From} to {To} by {Username} via action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }

        private async Task<bool> HandleDestructionAuthorizationTransitions(int entityId, int currentStatusId, string actionName,
            string username, string remarks, WorkflowTransitionDto transition)
        {
            var authorization = await _destructionAuthorizationRepository.FindById(entityId);
            if (authorization == null)
            {
                _logger.LogWarning("Destruction Authorization {Id} not found", entityId);
                return false;
            }

            if (authorization.CurrentStatusId != currentStatusId)
            {
                _logger.LogWarning("Destruction Authorization {Id} status mismatch. Current: {Current}, Expected: {Expected}",
                    entityId, authorization.CurrentStatusId, currentStatusId);
                return false;
            }

            var statusHistory = new DestructionAuthorizationStatusHistory
            {
                DestructionAuthorizationId = entityId,
                OldStatusId               = authorization.CurrentStatusId,
                OldStatusName             = authorization.CurrentStatusName,
                NewStatusId               = transition.ToStatusId,
                NewStatusName             = transition.ToStatus.Name,
                ChangedBy                 = username,
                ChangedAt                 = DateTime.Now,
                Remarks                   = remarks,
                IsDeleted                 = false
            };

            await _destructionAuthorizationStatusHistoryRepository.Create(statusHistory);

            authorization.CurrentStatusId   = transition.ToStatusId;
            authorization.CurrentStatusName = transition.ToStatus.Name;
            await _destructionAuthorizationRepository.Update(authorization);

            if (transition.SendNotification)
            {
                await SendTransitionNotification(
                    $"Destruction Authorization {authorization.ReferenceNumber}",
                    transition,
                    statusHistory.NewStatusName,
                    username, remarks);
            }

            _logger.LogInformation("Destruction Authorization {Id} transitioned from status {From} to {To} by user {Username} with action {Action}",
                entityId, currentStatusId, transition.ToStatusId, username, actionName);

            return true;
        }
    }
}