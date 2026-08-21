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
/// Covers ticket 03: RtgsCodeAtLastReturn snapshotting on ReturnForRevision (including the
/// Back-Office-origin auto-generated remark) and ResubmitForApproval's 3-origin routing, exercised
/// directly against <see cref="WorkflowServices.HandleWithdrawalTransitions"/> via the public
/// ExecuteTransition entry point. See ADR-0001 for the full routing rationale.
/// </summary>
public class ReturnOriginTrackingTests
{
    private const int RequiredRole = 1;
    private const int PendingVerificationStatusId = 42;

    private static WithdrawalRequest NewRequest(
        int statusTypeId, string statusName, string? rtgsCode = null,
        string? rtgsCodeAtLastReturn = null, string rtgsVerificationStatus = RtgsVerification.Pending)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = statusTypeId,
            StatusTypeName = statusName,
            RtgsCode = rtgsCode,
            RtgsCodeAtLastReturn = rtgsCodeAtLastReturn,
            RtgsVerificationStatus = rtgsVerificationStatus,
            TotalAmount = 10_000_000m,
        };

    private static WorkflowTransitionDto NewTransition(int toStatusId, string toStatusName)
        => new()
        {
            ToStatusId = toStatusId,
            ToStatus = new StatusTypeDto { Id = toStatusId, Name = toStatusName },
            RequiredRole = RequiredRole,
            SendNotification = false,
        };

    private static WorkflowServices BuildService(
        WithdrawalRequest request,
        WorkflowTransitionDto transition,
        List<WithdrawalRequestStatusHistory> createdHistory,
        WithdrawalRequestStatusHistory? lastReturn = null)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        requestRepo.Setup(r => r.Update(It.IsAny<WithdrawalRequest>())).ReturnsAsync(true);

        var historyRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        historyRepo
            .Setup(r => r.Create(It.IsAny<WithdrawalRequestStatusHistory>()))
            .Callback<WithdrawalRequestStatusHistory>(createdHistory.Add)
            .ReturnsAsync(true);
        historyRepo
            .Setup(r => r.FindMostRecentByNewStatusNameAsync(
                request.Id, WithdrawalStatusNames.ReturnedForRevision, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastReturn);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.StatusHistory).Returns(historyRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowClient = new Mock<IWorkflowServiceClient>();
        workflowClient
            .Setup(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()))
            .ReturnsAsync(transition);

        var statusResolver = new Mock<IWithdrawalStatusResolver>();
        statusResolver
            .Setup(r => r.GetStatusIdAsync(WithdrawalStatusNames.PendingVerification))
            .ReturnsAsync(PendingVerificationStatusId);

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
            statusResolver.Object);
    }

    private static Task<bool> ExecuteAsync(WorkflowServices service, WithdrawalRequest request, string actionName, string remarks = "")
        => service.ExecuteTransition(
            StatusConstants.Entity.WithdrawalRequest,
            request.Id,
            request.StatusTypeId,
            actionName,
            "tester",
            [RequiredRole],
            remarks,
            CancellationToken.None);

    [Fact]
    public async Task ReturnForRevision_From_ABM_Snapshots_RtgsCodeAtLastReturn_And_Keeps_Human_Remark()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification, rtgsCode: "RTGS-001");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision, "ABM found an unrelated issue");

        Assert.True(result);
        Assert.Equal("RTGS-001", request.RtgsCodeAtLastReturn);
        Assert.Equal(WithdrawalStatusNames.ReturnedForRevision, request.StatusTypeName);
        Assert.Equal("ABM found an unrelated issue", Assert.Single(createdHistory).Remarks);
    }

    [Fact]
    public async Task ReturnForRevision_From_CbApprovers_Own_Return_Leaves_RtgsCodeAtLastReturn_Untouched()
    {
        // Origin 1 (Pending Internal Approval) never has a confirmed/failed code to compare
        // against — snapshotting here would clobber whatever an earlier ABM/Back-Office return
        // already recorded for a later resubmit to compare against. See ADR-0001.
        var request = NewRequest(
            5, WithdrawalStatusNames.PendingInternalApproval,
            rtgsCode: "UNRELATED-CODE", rtgsCodeAtLastReturn: "PRIOR-ABM-SNAPSHOT");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision, "CB Approver's own return");

        Assert.True(result);
        Assert.Equal("PRIOR-ABM-SNAPSHOT", request.RtgsCodeAtLastReturn);
        Assert.Equal("CB Approver's own return", Assert.Single(createdHistory).Remarks);
    }

    [Fact]
    public async Task ReturnForRevision_From_BackOffice_Snapshots_Code_And_AutoGenerates_Remark_Ignoring_Human_Input()
    {
        var request = NewRequest(
            15, WithdrawalStatusNames.VerificationFailed,
            rtgsCode: "RTGS-002", rtgsVerificationStatus: RtgsVerification.AmountMismatch);
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision, "human-entered comment");

        Assert.True(result);
        Assert.Equal("RTGS-002", request.RtgsCodeAtLastReturn);
        Assert.Equal(
            "RTGS code RTGS-002 failed confirmation: AmountMismatch — Penalty #7 applied",
            Assert.Single(createdHistory).Remarks);
    }

    [Fact]
    public async Task ResubmitForApproval_From_ABM_Origin_With_Same_Code_Skips_Straight_To_PendingVerification()
    {
        var request = NewRequest(
            30, WithdrawalStatusNames.ReturnedForRevision,
            rtgsCode: "SAME-CODE", rtgsCodeAtLastReturn: "SAME-CODE",
            rtgsVerificationStatus: RtgsVerification.Confirmed);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "abm-user",
            ChangedAt = DateTime.UtcNow,
        };
        // The WorkflowService-configured row always points at Pending Internal Approval — the
        // same-code bypass must override this, not follow it.
        var transition = NewTransition(99, WithdrawalStatusNames.PendingInternalApproval);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory, lastReturn);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ResubmitForApproval);

        Assert.True(result);
        Assert.Equal(PendingVerificationStatusId, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingVerification, request.StatusTypeName);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task ResubmitForApproval_From_ABM_Origin_With_Different_Code_Follows_Normal_Path_And_Resets_Verification()
    {
        var request = NewRequest(
            30, WithdrawalStatusNames.ReturnedForRevision,
            rtgsCode: "NEW-CODE", rtgsCodeAtLastReturn: "OLD-CODE",
            rtgsVerificationStatus: RtgsVerification.Confirmed);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingVerification,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "abm-user",
            ChangedAt = DateTime.UtcNow,
        };
        var transition = NewTransition(99, WithdrawalStatusNames.PendingInternalApproval);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory, lastReturn);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ResubmitForApproval);

        Assert.True(result);
        Assert.Equal(99, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingInternalApproval, request.StatusTypeName);
        Assert.Equal(RtgsVerification.Pending, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task ResubmitForApproval_From_BackOffice_Origin_Never_Skips_Ahead_Even_With_Identical_Code()
    {
        var request = NewRequest(
            30, WithdrawalStatusNames.ReturnedForRevision,
            rtgsCode: "BAD-CODE", rtgsCodeAtLastReturn: "BAD-CODE",
            rtgsVerificationStatus: RtgsVerification.AmountMismatch);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.VerificationFailed,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "back-office-user",
            ChangedAt = DateTime.UtcNow,
        };
        var transition = NewTransition(99, WithdrawalStatusNames.PendingInternalApproval);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory, lastReturn);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ResubmitForApproval);

        Assert.True(result);
        Assert.Equal(99, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingInternalApproval, request.StatusTypeName);
    }

    [Fact]
    public async Task ResubmitForApproval_From_CbApprovers_Own_Return_Behaves_As_Today()
    {
        var request = NewRequest(30, WithdrawalStatusNames.ReturnedForRevision, rtgsCode: null);
        var lastReturn = new WithdrawalRequestStatusHistory
        {
            OldStatusName = WithdrawalStatusNames.PendingInternalApproval,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedBy = "cb-approver",
            ChangedAt = DateTime.UtcNow,
        };
        var transition = NewTransition(99, WithdrawalStatusNames.PendingInternalApproval);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory, lastReturn);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.ResubmitForApproval);

        Assert.True(result);
        Assert.Equal(99, request.StatusTypeId);
        Assert.Equal(WithdrawalStatusNames.PendingInternalApproval, request.StatusTypeName);
    }

    [Fact]
    public async Task RtgsCodeAtLastReturn_Is_Overwritten_Not_Appended_Across_Two_Successive_Returns()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification, rtgsCode: "CODE-1");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision);
        var createdHistory = new List<WithdrawalRequestStatusHistory>();
        var service = BuildService(request, transition, createdHistory);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision, "first return");
        Assert.Equal("CODE-1", request.RtgsCodeAtLastReturn);

        // Simulate the request cycling back to Pending Verification with an amended code before
        // the ABM returns it a second time.
        request.StatusTypeId = 10;
        request.StatusTypeName = WithdrawalStatusNames.PendingVerification;
        request.RtgsCode = "CODE-2";

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision, "second return");

        Assert.Equal("CODE-2", request.RtgsCodeAtLastReturn);
    }
}
