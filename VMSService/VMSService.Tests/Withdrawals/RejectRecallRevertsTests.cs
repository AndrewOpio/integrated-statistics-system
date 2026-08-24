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
/// Covers ticket 04: RejectRecall (Currency Approver, from Recall Verified) reverts the request
/// directly to whichever status it was in before the recall was requested — a single-actor, direct
/// status mutation, same lookback technique as ticket 03's return-origin tracking. See ADR-0001's
/// "Recall's reject also reverts, using the same technique" section.
/// </summary>
public class RejectRecallRevertsTests
{
    private const int RequiredRole = 1;
    private const int RecallVerifiedStatusId = 60;
    private const int RecallRejectedPlaceholderStatusId = 61;

    private static WithdrawalRequest NewRequest()
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = RecallVerifiedStatusId,
            StatusTypeName = WithdrawalStatusNames.RecallVerified,
            RecallRtgsCode = null,
            RecallRtgsDate = null,
            RecallRtgsPending = false,
            TotalAmount = 10_000_000m,
        };

    private static WorkflowTransitionDto NewTransition()
        => new()
        {
            ToStatusId = RecallRejectedPlaceholderStatusId,
            ToStatus = new StatusTypeDto { Id = RecallRejectedPlaceholderStatusId, Name = "Recall Rejected" },
            RequiredRole = RequiredRole,
            SendNotification = false,
        };

    private static (WorkflowServices Service, List<WithdrawalRequestStatusHistory> CreatedHistory) BuildService(
        WithdrawalRequest request, WithdrawalRequestStatusHistory? lastRecallTransition)
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
        historyRepo
            .Setup(r => r.FindMostRecentByNewStatusNameAsync(
                request.Id, WithdrawalStatusNames.RecallRequested, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastRecallTransition);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.StatusHistory).Returns(historyRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowClient = new Mock<IWorkflowServiceClient>();
        workflowClient
            .Setup(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()))
            .ReturnsAsync(NewTransition());

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
            Mock.Of<IStockPositionService>());

        return (service, createdHistory);
    }

    private static Task<bool> RejectAsync(WorkflowServices service, WithdrawalRequest request)
        => service.ExecuteTransition(
            StatusConstants.Entity.WithdrawalRequest,
            request.Id,
            request.StatusTypeId,
            WithdrawalStatusActionNames.RejectRecall,
            "currency-approver",
            [RequiredRole],
            "not approved",
            CancellationToken.None);

    [Theory]
    [InlineData(70, "Pending RTGS Code")]
    [InlineData(71, "Pending RTGS Confirmation")]
    [InlineData(72, "Pending Exceptional Approval")]
    [InlineData(73, "Pending Verification")]
    [InlineData(74, "Pending Approval")]
    public async Task RejectRecall_Reverts_To_Exact_Originating_Status(int originStatusId, string originStatusName)
    {
        var request = NewRequest();
        var lastRecallTransition = new WithdrawalRequestStatusHistory
        {
            OldStatusId = originStatusId,
            OldStatusName = originStatusName,
            NewStatusId = 99,
            NewStatusName = WithdrawalStatusNames.RecallRequested,
            ChangedBy = "cb-maker",
            ChangedAt = DateTime.UtcNow,
        };
        var (service, _) = BuildService(request, lastRecallTransition);

        var result = await RejectAsync(service, request);

        Assert.True(result);
        Assert.Equal(originStatusId, request.StatusTypeId);
        Assert.Equal(originStatusName, request.StatusTypeName);
    }

    [Fact]
    public async Task RejectRecall_Does_Not_Touch_RecallRtgsFields()
    {
        var request = NewRequest();
        var lastRecallTransition = new WithdrawalRequestStatusHistory
        {
            OldStatusId = 70,
            OldStatusName = WithdrawalStatusNames.PendingRTGSVerification,
            NewStatusId = 99,
            NewStatusName = WithdrawalStatusNames.RecallRequested,
            ChangedBy = "cb-maker",
            ChangedAt = DateTime.UtcNow,
        };
        var (service, _) = BuildService(request, lastRecallTransition);

        await RejectAsync(service, request);

        Assert.Null(request.RecallRtgsCode);
        Assert.Null(request.RecallRtgsDate);
        Assert.False(request.RecallRtgsPending);
    }

    [Fact]
    public async Task RejectRecall_Writes_A_Single_History_Row_Straight_To_The_Origin_Status()
    {
        var request = NewRequest();
        var lastRecallTransition = new WithdrawalRequestStatusHistory
        {
            OldStatusId = 70,
            OldStatusName = WithdrawalStatusNames.PendingRTGSVerification,
            NewStatusId = 99,
            NewStatusName = WithdrawalStatusNames.RecallRequested,
            ChangedBy = "cb-maker",
            ChangedAt = DateTime.UtcNow,
        };
        var (service, createdHistory) = BuildService(request, lastRecallTransition);

        await RejectAsync(service, request);

        var row = Assert.Single(createdHistory);
        Assert.Equal(WithdrawalStatusNames.RecallVerified, row.OldStatusName);
        Assert.Equal(WithdrawalStatusNames.PendingRTGSVerification, row.NewStatusName);
    }

    [Fact]
    public async Task RejectRecall_Reverting_To_PendingRtgsCode_Does_Not_ReRun_Submission_Classification()
    {
        // Regression coverage: the CB-approval submission-classification side effect in
        // HandleWithdrawalTransitions used to key off the destination status name alone (Pending
        // RTGS Code), not the action. Landing back there via a rejected recall would trip it too —
        // wrongly deriving RequiresSpecialApproval/SpecialApprovalReason from the Currency Approver's
        // reject remarks, and (as here) spuriously failing the whole action as "late" once the
        // original collection date has passed during the recall cycle.
        var request = NewRequest();
        request.CollectionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var lastRecallTransition = new WithdrawalRequestStatusHistory
        {
            OldStatusId = 70,
            OldStatusName = WithdrawalStatusNames.PendingRTGSVerification,
            NewStatusId = 99,
            NewStatusName = WithdrawalStatusNames.RecallRequested,
            ChangedBy = "cb-maker",
            ChangedAt = DateTime.UtcNow,
        };
        var (service, _) = BuildService(request, lastRecallTransition);

        var result = await RejectAsync(service, request);

        Assert.True(result);
        Assert.Equal(WithdrawalStatusNames.PendingRTGSVerification, request.StatusTypeName);
        Assert.False(request.RequiresSpecialApproval);
        Assert.Null(request.SpecialApprovalReason);
    }
}
