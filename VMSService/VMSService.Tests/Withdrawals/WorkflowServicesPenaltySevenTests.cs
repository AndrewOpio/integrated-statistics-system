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
/// Penalty #7 ("Error on the letter of intent and RTGS"), both its satisfiable
/// triggers: (a) <c>RtgsConfirmationFailed</c> firing — now also writes
/// <c>RtgsVerificationStatus = Failed</c> as part of the transition, since that's the only live
/// path that ever writes a failure outcome (VerifyRtgs has no UI entry point); (b) the RTGS code
/// differing from the prior return's snapshot at a subsequent ReturnForRevision, either origin.
/// </summary>
public class WorkflowServicesPenaltySevenTests
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
            TransactionRefNumber = "REF-PENALTY7-TEST",
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

    private static (WorkflowServices Service, FakeAppliedPenaltyRepository AppliedPenalties) BuildService(
        WithdrawalRequest request, WorkflowTransitionDto transition)
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
            .ReturnsAsync([transition]);

        var penaltyRepository = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.ErrorOnLetterOfIntentAndRtgs);
        var appliedPenaltyRepository = new FakeAppliedPenaltyRepository();

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
            penaltyRepository,
            appliedPenaltyRepository);

        return (service, appliedPenaltyRepository);
    }

    private static Task<bool> ExecuteAsync(WorkflowServices service, WithdrawalRequest request, string actionName)
        => service.ExecuteTransition(
            StatusConstants.Entity.WithdrawalRequest,
            request.Id,
            request.StatusTypeId,
            actionName,
            "tester",
            [RequiredRole],
            "remarks",
            CancellationToken.None);

    // ── 7a: RtgsConfirmationFailed ───────────────────────────────────────────

    [Fact]
    public async Task RtgsConfirmationFailed_Writes_Failed_Status_And_Applies_Penalty7()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingWithdrawalConfirmation, rtgsCode: "RTGS-001");
        var transition = NewTransition(20, WithdrawalStatusNames.VerificationFailed, WithdrawalStatusActionNames.RtgsConfirmationFailed);
        var (service, appliedPenalties) = BuildService(request, transition);

        var result = await ExecuteAsync(service, request, WithdrawalStatusActionNames.RtgsConfirmationFailed);

        Assert.True(result);
        Assert.Equal(RtgsVerification.Failed, request.RtgsVerificationStatus);
        Assert.Single(appliedPenalties.Rows, a => a.PenaltyId == 1);
    }

    [Fact]
    public async Task RtgsConfirmationFailed_Does_Not_Stack_Penalty7_On_A_Second_Call()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingWithdrawalConfirmation, rtgsCode: "RTGS-001");
        var transition = NewTransition(20, WithdrawalStatusNames.VerificationFailed, WithdrawalStatusActionNames.RtgsConfirmationFailed);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.RtgsConfirmationFailed);
        await ExecuteAsync(service, request, WithdrawalStatusActionNames.RtgsConfirmationFailed);

        Assert.Single(appliedPenalties.Rows);
    }

    [Fact]
    public async Task RtgsConfirmationFailed_Never_Resurrects_A_Manually_Removed_Penalty7_On_Retrigger()
    {
        // A penalty removed by hand (soft-deleted, e.g. waived by Back Office) must stay removed —
        // FindBySourceId alone would miss the deleted row and let Apply's upsert un-delete it.
        var request = NewRequest(10, WithdrawalStatusNames.PendingWithdrawalConfirmation, rtgsCode: "RTGS-001");
        var transition = NewTransition(20, WithdrawalStatusNames.VerificationFailed, WithdrawalStatusActionNames.RtgsConfirmationFailed);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.RtgsConfirmationFailed);
        var applied = Assert.Single(appliedPenalties.Rows);
        await appliedPenalties.Remove(applied.Id);
        Assert.True(applied.IsDeleted);

        // Re-fetch a fresh copy of the same request/transition and re-trigger the same event.
        request.StatusTypeId = 10;
        request.StatusTypeName = WithdrawalStatusNames.PendingWithdrawalConfirmation;
        await ExecuteAsync(service, request, WithdrawalStatusActionNames.RtgsConfirmationFailed);

        Assert.True(applied.IsDeleted);
    }

    // ── 7b: RTGS code changed since the prior return ────────────────────────

    [Fact]
    public async Task ReturnForRevision_From_Abm_With_Changed_Code_Applies_Penalty7()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification,
            rtgsCode: "CODE-2", rtgsCodeAtLastReturn: "CODE-1");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision);

        Assert.Single(appliedPenalties.Rows, a => a.PenaltyId == 1);
        Assert.Equal("CODE-2", request.RtgsCodeAtLastReturn);
    }

    [Fact]
    public async Task ReturnForRevision_From_Abm_With_Unchanged_Code_Does_Not_Apply_Penalty7()
    {
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification,
            rtgsCode: "CODE-1", rtgsCodeAtLastReturn: "CODE-1");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision);

        Assert.Empty(appliedPenalties.Rows);
    }

    [Fact]
    public async Task ReturnForRevision_On_A_Requests_First_Return_Never_Applies_Penalty7()
    {
        // Nothing to compare the code against yet — RtgsCodeAtLastReturn is null.
        var request = NewRequest(10, WithdrawalStatusNames.PendingVerification,
            rtgsCode: "CODE-1", rtgsCodeAtLastReturn: null);
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision);

        Assert.Empty(appliedPenalties.Rows);
        Assert.Equal("CODE-1", request.RtgsCodeAtLastReturn);
    }

    [Fact]
    public async Task ReturnForRevision_From_BackOffice_With_Changed_Code_Applies_Penalty7()
    {
        var request = NewRequest(15, WithdrawalStatusNames.VerificationFailed,
            rtgsCode: "CODE-2", rtgsCodeAtLastReturn: "CODE-1", rtgsVerificationStatus: RtgsVerification.Failed);
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision);

        Assert.Single(appliedPenalties.Rows, a => a.PenaltyId == 1);
    }

    [Fact]
    public async Task ReturnForRevision_From_CbApprovers_Own_Return_Never_Applies_Penalty7()
    {
        // Origin 1 (Pending Internal Approval) never has a confirmed/failed code to compare — the
        // whole snapshot-and-compare block is skipped regardless of how different the codes are.
        var request = NewRequest(5, WithdrawalStatusNames.PendingInternalApproval,
            rtgsCode: "UNRELATED-CODE", rtgsCodeAtLastReturn: "PRIOR-SNAPSHOT");
        var transition = NewTransition(20, WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusActionNames.ReturnForRevision);
        var (service, appliedPenalties) = BuildService(request, transition);

        await ExecuteAsync(service, request, WithdrawalStatusActionNames.ReturnForRevision);

        Assert.Empty(appliedPenalties.Rows);
    }
}
