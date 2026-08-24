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
/// Covers ticket 03's RtgsCodeAtLastReturn snapshotting on ReturnForRevision, including the
/// Back-Office-origin auto-generated remark, exercised directly against
/// <see cref="WorkflowServices.HandleWithdrawalTransitions"/> via the public ExecuteTransition
/// entry point. The 3-origin ResubmitForApproval routing this ticket originally added here moved
/// to <c>CbWithdrawRequestsController.ExecuteAction</c> (see CbWithdrawRequestsControllerTests) —
/// ResubmitForApproval itself always follows the normal configured transition; the ABM-same-code
/// skip-ahead only happens later, at the CB Approver's own approval action. See ADR-0001.
/// </summary>
public class ReturnOriginTrackingTests
{
    private const int RequiredRole = 1;

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

    private static WorkflowTransitionDto NewTransition(int toStatusId, string toStatusName, string actionName)
        => new()
        {
            ActionName = actionName,
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
            .Setup(c => c.GetAvailableTransitionsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([transition]);

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
            Mock.Of<IStockPositionService>());
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
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
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
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
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
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
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
    public async Task RtgsCodeAtLastReturn_Is_Overwritten_Not_Appended_Across_Two_Successive_Returns()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification, rtgsCode: "CODE-1");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
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
