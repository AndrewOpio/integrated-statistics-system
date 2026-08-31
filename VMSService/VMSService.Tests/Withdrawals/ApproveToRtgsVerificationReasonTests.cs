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
using VMSService.Core.Services.Submissions;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// The ApproveToRTGSVerification transition used to silently derive RequiresSpecialApproval/
/// SpecialApprovalReason from the general workflow comment (`remarks`) whenever the flag wasn't
/// already set — conflating a workflow remark with a dedicated approval reason. CB Withdraw
/// Requests Controller's ExecuteAction now enforces this properly before the transition is ever
/// attempted, making the derivation both wrong and (via that one caller) unreachable — but it was
/// still sitting in WorkflowServices as a landmine for any future/other caller. This covers its
/// removal: the transition must never copy remarks into SpecialApprovalReason, regardless of
/// classification outcome.
/// </summary>
public class ApproveToRtgsVerificationReasonTests
{
    private const int RequiredRole = 1;
    private const int PendingRtgsVerificationStatusId = 3;

    private static WithdrawalRequest NewRequest(DateOnly collectionDate, bool requiresSpecialApproval, string? reason) => new()
    {
        Id = Guid.NewGuid(),
        StatusTypeId = 1,
        StatusTypeName = WithdrawalStatusNames.PendingInternalApproval,
        CollectionDate = collectionDate,
        RequiresSpecialApproval = requiresSpecialApproval,
        SpecialApprovalReason = reason,
        TotalAmount = 10_000_000m,
    };

    private static WorkflowTransitionDto ApproveTransition() => new()
    {
        ActionName = WithdrawalStatusActionNames.ApproveToRTGSVerification,
        ToStatusId = PendingRtgsVerificationStatusId,
        ToStatus = new StatusTypeDto { Id = PendingRtgsVerificationStatusId, Name = WithdrawalStatusNames.PendingRTGSVerification },
        RequiredRole = RequiredRole,
        SendNotification = false,
    };

    private static WorkflowServices BuildService(WithdrawalRequest request, WorkflowTransitionDto entryTransition)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        requestRepo.Setup(r => r.Update(It.IsAny<WithdrawalRequest>())).ReturnsAsync(true);

        var historyRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        historyRepo
            .Setup(r => r.Create(It.IsAny<WithdrawalRequestStatusHistory>()))
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

        // Forced true unconditionally — removes dependence on which real weekday "yesterday"
        // happens to be when these tests run, same technique used elsewhere in this suite.
        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository
            .Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync(new PublicHoliday { Date = DateOnly.FromDateTime(DateTime.Today), Name = "Forced" });

        return new WorkflowServices(
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
            publicHolidayRepository.Object,
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
    }

    private static Task<bool> ExecuteAsync(WorkflowServices service, WithdrawalRequest request, string remarks)
        => service.ExecuteTransition(
            StatusConstants.Entity.WithdrawalRequest,
            request.Id,
            request.StatusTypeId,
            WithdrawalStatusActionNames.ApproveToRTGSVerification,
            "cb-approver",
            [RequiredRole],
            remarks,
            CancellationToken.None);

    [Fact]
    public async Task Approving_An_Unflagged_Request_Never_Copies_The_Comment_Into_The_Reason()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: false, reason: null);
        var service = BuildService(request, ApproveTransition());

        var result = await ExecuteAsync(service, request, "Looks good, approving now");

        Assert.True(result);
        Assert.NotEqual("Looks good, approving now", request.SpecialApprovalReason);
        Assert.Null(request.SpecialApprovalReason);
    }

    [Fact]
    public async Task Approving_An_Already_Flagged_Request_Leaves_Its_Real_Reason_Untouched()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, requiresSpecialApproval: true, reason: "Genuine same-day reason");
        var service = BuildService(request, ApproveTransition());

        var result = await ExecuteAsync(service, request, "Approving as discussed");

        Assert.True(result);
        Assert.Equal("Genuine same-day reason", request.SpecialApprovalReason);
        Assert.NotEqual("Approving as discussed", request.SpecialApprovalReason);
    }

    [Fact]
    public async Task Approving_After_The_Collection_Date_Has_Passed_Is_Rejected()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var request = NewRequest(yesterday, requiresSpecialApproval: false, reason: null);
        var service = BuildService(request, ApproveTransition());

        var result = await ExecuteAsync(service, request, "approving late");

        Assert.False(result);
    }
}
