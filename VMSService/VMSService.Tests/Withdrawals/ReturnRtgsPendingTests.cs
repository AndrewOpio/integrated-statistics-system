using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Client;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AgentDetailStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories;
using VMSService.Core.Repositories.EodDiscrepancyReports;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.MonthlySurpluses;
using VMSService.Core.Repositories.MonthlySurplusStatusHistories;
using VMSService.Core.Repositories.MonthlyShortages;
using VMSService.Core.Repositories.MonthlyShortageStatusHistories;
using VMSService.Core.Repositories.MonthlyShortageReimbursements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories;
using VMSService.Core.Repositories.BankingSurpluses;
using VMSService.Core.Repositories.BankingSurplusStatusHistories;
using VMSService.Core.Repositories.BankingShortages;
using VMSService.Core.Repositories.BankingShortageStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizationStatusHistories;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Repositories.DestructionRequestStatusHistories;
using VMSService.Core.Repositories.DestructionRequests;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.StockTakes;
using VMSService.Core.Repositories.StockTakeStatusHistories;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Repositories.StockProcessingPlanStatusHistories;
using VMSService.Core.Repositories.StockProcessingSessions;
using VMSService.Core.Repositories.StockProcessingSessionStatusHistories;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Repositories.CfbTopupRequests;
using VMSService.Core.Repositories.CfbTopupStatusHistories;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.BoardSessionSurplusShortages;
using VMSService.Core.Repositories.BoardSessionSurplusShortageStatusHistories;
using VMSService.Core.Repositories.BagMovements;
using VMSService.Core.Repositories.BagMovementStatusHistories;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Covers ticket 05: the <c>ReturnRtgsPending</c> field on the Reject family. <c>RejectExceptional</c>
/// reads it off <c>RtgsVerificationStatus</c>; <c>Reject</c> sets it unconditionally true (RTGS is
/// always confirmed by the time either reject can happen — see CONTEXT.md's "Reject" entry and
/// ticket 34). Both stop at their intermediate status regardless of the value; there's no
/// auto-complete branch (removed after ticket 34's domain review found it structurally unreachable).
/// </summary>
public class ReturnRtgsPendingTests
{
    private const int RequiredRole = 1;
    private const int PendingExceptionalApprovalStatusId = 40;
    private const int ExceptionalApprovalRejectedStatusId = 41;
    private const int PendingApprovalStatusId = 50;
    private const int RejectedStatusId = 51;

    private static WithdrawalRequest NewExceptionalRequest(string rtgsVerificationStatus)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = PendingExceptionalApprovalStatusId,
            StatusTypeName = WithdrawalStatusNames.PendingExceptionalApproval,
            RtgsVerificationStatus = rtgsVerificationStatus,
            ReturnRtgsPending = false,
            ReturnRtgsCode = null,
            ReturnRtgsDate = null,
            TotalAmount = 10_000_000m,
        };

    private static WithdrawalRequest NewApprovalRequest()
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = PendingApprovalStatusId,
            StatusTypeName = WithdrawalStatusNames.PendingApproval,
            RtgsVerificationStatus = RtgsVerification.Confirmed,
            ReturnRtgsPending = false,
            ReturnRtgsCode = null,
            ReturnRtgsDate = null,
            TotalAmount = 10_000_000m,
        };

    private static WorkflowTransitionDto RejectExceptionalTransition()
        => new()
        {
            ActionName = WithdrawalStatusActionNames.RejectExceptional,
            ToStatusId = ExceptionalApprovalRejectedStatusId,
            ToStatus = new StatusTypeDto { Id = ExceptionalApprovalRejectedStatusId, Name = WithdrawalStatusNames.RejectExceptionalApproval },
            RequiredRole = RequiredRole,
            SendNotification = false,
        };

    private static WorkflowTransitionDto RejectTransition()
        => new()
        {
            ActionName = WithdrawalStatusActionNames.Reject,
            ToStatusId = RejectedStatusId,
            ToStatus = new StatusTypeDto { Id = RejectedStatusId, Name = WithdrawalStatusNames.Rejected },
            RequiredRole = RequiredRole,
            SendNotification = false,
        };

    private static (WorkflowServices Service, List<WithdrawalRequestStatusHistory> CreatedHistory, Mock<IWorkflowServiceClient> WorkflowClient) BuildService(
        WithdrawalRequest request, WorkflowTransitionDto entryTransition)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        requestRepo.Setup(r => r.Update(It.IsAny<WithdrawalRequest>())).ReturnsAsync(true);

        var historyRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        historyRepo
            .Setup(r => r.Create(It.IsAny<WithdrawalRequestStatusHistory>()))
            .Callback<WithdrawalRequestStatusHistory>(createdHistory.Add)
            .ReturnsAsync(true);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.StatusHistory).Returns(historyRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowClient = new Mock<IWorkflowServiceClient>();
        workflowClient
            .Setup(c => c.GetAvailableTransitionsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([entryTransition]);

        var service = new WorkflowServices(
            Mock.Of<IApprovedCurrencyProcurementRepository>(),
            Mock.Of<IApprovedCurrencyStatusHistoryRepository>(),
            Mock.Of<ILogger<WorkflowServices>>(),
            Mock.Of<IExPrintMintRepository>(),
            Mock.Of<IReceivedStockRepository>(),
            Mock.Of<IExPrintMintStatusHistoryRepository>(),
            Mock.Of<IStockProcessingPlanRepository>(),
            Mock.Of<IStockProcessingPlanStatusHistoryRepository>(),
            Mock.Of<IStockProcessingSessionRepository>(),
            Mock.Of<IStockProcessingSessionStatusHistoryRepository>(),
            Mock.Of<IReceivedStockStatusHistoryRepository>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStockMovementStatusHistoryRepository>(),
            Mock.Of<IStockMovementDiscrepancyRepository>(),
            workflowClient.Object,
            Mock.Of<INotificationServiceClient>(),
            Mock.Of<IIdentityService>(),
            Mock.Of<IWithdrawalTransactionRefNumberGenerator>(),
            uow.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IAgentDetailStatusHistoryRepository>(),
            Mock.Of<IDepositUnitOfWork>(),
            Mock.Of<IDestructionRequestRepository>(),
            Mock.Of<IDestructionRequestStatusHistoryRepository>(),
            Mock.Of<IDestructionAuthorizationRepository>(),
            Mock.Of<IDestructionAuthorizationStatusHistoryRepository>(),
            Mock.Of<IEodDiscrepancyReportRepository>(),
            Mock.Of<IEodDiscrepancyReportStatusHistoryRepository>(),
            Mock.Of<IMonthlySurplusRepository>(),
            Mock.Of<IMonthlySurplusStatusHistoryRepository>(),
            Mock.Of<IMonthlyShortageRepository>(),
            Mock.Of<IMonthlyShortageStatusHistoryRepository>(),
            Mock.Of<IMonthlyCommercialBankSettlementRepository>(),
            Mock.Of<IMonthlyCommercialBankSettlementStatusHistoryRepository>(),
            Mock.Of<IBankingSurplusRepository>(),
            Mock.Of<IBankingSurplusStatusHistoryRepository>(),
            Mock.Of<IBankingShortageRepository>(),
            Mock.Of<IBankingShortageStatusHistoryRepository>(),
            Mock.Of<ITopupRequestRepository>(),
            Mock.Of<ITopupRequestStatusHistoryRepository>(),
            Mock.Of<IPublicHolidayRepository>(),
            Mock.Of<IBoardSessionSurplusShortageRepository>(),
            Mock.Of<IBoardSessionSurplusShortageStatusHistoryRepository>(),
            Mock.Of<IBagMovementRepository>(),
            Mock.Of<IBagMovementStatusHistoryRepository>(),
            Mock.Of<IBagRepository>(),
            Mock.Of<IMonthlyShortageReimbursementRepository>(),
            Mock.Of<IMonthlyShortageReimbursementStatusHistoryRepository>(),
            Mock.Of<IStockTakeRepository>(),
            Mock.Of<IStockTakeStatusHistoryRepository>(),
            Mock.Of<IStockPositionService>(),
            Mock.Of<IPenaltyRepository>(),
            Mock.Of<IAppliedPenaltyRepository>());

        return (service, createdHistory, workflowClient);
    }

    private static Task<bool> ExecuteAsync(WorkflowServices service, WithdrawalRequest request, string actionName)
        => service.ExecuteTransition(
            StatusConstants.Entity.WithdrawalRequest,
            request.Id,
            request.StatusTypeId,
            actionName,
            "back-office",
            [RequiredRole],
            "not approved",
            CancellationToken.None);

    [Fact]
    public async Task RejectExceptional_When_Confirmed_Sets_Pending_True_And_Stops_At_Intermediate_Status()
    {
        var request = NewExceptionalRequest(RtgsVerification.Confirmed);
        var (service, createdHistory, workflowClient) = BuildService(request, RejectExceptionalTransition());

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.RejectExceptional);

        Assert.True(result);
        Assert.True(request.ReturnRtgsPending);
        Assert.Equal(ExceptionalApprovalRejectedStatusId, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.RejectExceptionalApproval, request.StatusTypeName);
        Assert.Null(request.ReturnRtgsCode);
        Assert.Null(request.ReturnRtgsDate);
        var row = Assert.Single(createdHistory);
        Assert.Equal(WithdrawalStatusNames.RejectExceptionalApproval, row.NewStatusName);
        workflowClient.Verify(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task RejectExceptional_When_Not_Confirmed_Still_Stops_At_Intermediate_Status_No_AutoComplete()
    {
        // Defensive/historical-data case only (ticket 33 leaves old non-Confirmed values on existing
        // rows untouched) — a real request can't reach Pending Exceptional Approval this way (ticket 34).
        // There's no auto-complete branch: ReturnRtgsPending is just false, and the request stops at
        // the same intermediate status as the Confirmed case, same as Reject always does.
        var request = NewExceptionalRequest(RtgsVerification.Pending);
        var (service, createdHistory, workflowClient) = BuildService(request, RejectExceptionalTransition());

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.RejectExceptional);

        Assert.True(result);
        Assert.False(request.ReturnRtgsPending);
        Assert.Equal(ExceptionalApprovalRejectedStatusId, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.RejectExceptionalApproval, request.StatusTypeName);
        Assert.Null(request.ReturnRtgsCode);
        Assert.Null(request.ReturnRtgsDate);
        var row = Assert.Single(createdHistory);
        Assert.Equal(WithdrawalStatusNames.RejectExceptionalApproval, row.NewStatusName);
        workflowClient.Verify(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Reject_Always_Sets_Pending_True_And_Stops_At_Rejected()
    {
        var request = NewApprovalRequest();
        var (service, createdHistory, workflowClient) = BuildService(request, RejectTransition());

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.Reject);

        Assert.True(result);
        Assert.True(request.ReturnRtgsPending);
        Assert.Equal(RejectedStatusId, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.Rejected, request.StatusTypeName);
        var row = Assert.Single(createdHistory);
        Assert.Equal(WithdrawalStatusNames.Rejected, row.NewStatusName);
        workflowClient.Verify(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()), Times.Never);
    }
}
