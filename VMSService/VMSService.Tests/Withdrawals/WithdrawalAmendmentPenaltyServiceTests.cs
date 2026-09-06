using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.BackgroundServices;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Penalty #5 ("Failure to amend in time"), applies-case: the ABM returned for revision
/// before 11:00 EAT and the request is still sitting unresubmitted at 11:00. The waived case (ABM
/// returned at/after 11:00) is a computed display flag instead — see
/// WithdrawalRequestsControllerAmendmentWaivedFlagTests — and is never exercised here.
/// </summary>
public class WithdrawalAmendmentPenaltyServiceTests
{
    private static WithdrawalRequest NewRequest(string? transactionRefNumber = "REF-5-TEST")
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 20,
            StatusTypeName = WithdrawalStatusNames.ReturnedForRevision,
            TransactionRefNumber = transactionRefNumber,
            TotalAmount = 10_000_000m,
        };

    private static WithdrawalRequestStatusHistory ReturnEntry(string oldStatusName, DateTime changedAtUtc)
        => new()
        {
            OldStatusName = oldStatusName,
            NewStatusName = WithdrawalStatusNames.ReturnedForRevision,
            ChangedAt = changedAtUtc,
        };

    private static Mock<IWithdrawalUnitOfWork> BuildUow(
        List<WithdrawalRequest> candidates, Dictionary<Guid, WithdrawalRequestStatusHistory?> lastReturns)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.GetReturnedForRevisionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(candidates);

        var historyRepo = new Mock<IWithdrawalRequestStatusHistoryRepository>();
        foreach (var (id, entry) in lastReturns)
        {
            historyRepo
                .Setup(r => r.FindMostRecentByNewStatusNameAsync(id, WithdrawalStatusNames.ReturnedForRevision, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entry);
        }

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.StatusHistory).Returns(historyRepo.Object);
        return uow;
    }

    [Fact]
    public async Task Applies_Penalty_When_Abm_Returned_Before_11_And_Still_Unresubmitted()
    {
        var request = NewRequest();
        var lastReturn = ReturnEntry(WithdrawalStatusNames.PendingVerification, new DateTime(2024, 1, 1, 6, 0, 0, DateTimeKind.Utc)); // 09:00 EAT
        var uow = BuildUow([request], new() { [request.Id] = lastReturn });
        var penaltyRepo = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.FailureToAmendInTime);
        var appliedPenalties = new FakeAppliedPenaltyRepository();

        var count = await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Single(appliedPenalties.Rows, a => a.PenaltyId == 1 && a.SourceId == request.TransactionRefNumber);
    }

    [Fact]
    public async Task Does_Not_Apply_When_Abm_Returned_At_Or_After_11()
    {
        var request = NewRequest();
        var lastReturn = ReturnEntry(WithdrawalStatusNames.PendingVerification, new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc)); // 12:00 EAT
        var uow = BuildUow([request], new() { [request.Id] = lastReturn });
        var penaltyRepo = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.FailureToAmendInTime);
        var appliedPenalties = new FakeAppliedPenaltyRepository();

        var count = await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Empty(appliedPenalties.Rows);
    }

    [Fact]
    public async Task Does_Not_Apply_When_The_Return_Was_Back_Office_Origin_Not_Abm()
    {
        var request = NewRequest();
        var lastReturn = ReturnEntry(WithdrawalStatusNames.VerificationFailed, new DateTime(2024, 1, 1, 6, 0, 0, DateTimeKind.Utc)); // 09:00 EAT
        var uow = BuildUow([request], new() { [request.Id] = lastReturn });
        var penaltyRepo = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.FailureToAmendInTime);
        var appliedPenalties = new FakeAppliedPenaltyRepository();

        var count = await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Does_Not_Reapply_When_Already_Applied()
    {
        var request = NewRequest();
        var lastReturn = ReturnEntry(WithdrawalStatusNames.PendingVerification, new DateTime(2024, 1, 1, 6, 0, 0, DateTimeKind.Utc));
        var uow = BuildUow([request], new() { [request.Id] = lastReturn });
        var penaltyRepo = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.FailureToAmendInTime);
        var appliedPenalties = new FakeAppliedPenaltyRepository();

        await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);
        var secondRunCount = await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(0, secondRunCount);
        Assert.Single(appliedPenalties.Rows);
    }

    [Fact]
    public async Task Skips_Requests_With_No_TransactionRefNumber()
    {
        var request = NewRequest(transactionRefNumber: null);
        var lastReturn = ReturnEntry(WithdrawalStatusNames.PendingVerification, new DateTime(2024, 1, 1, 6, 0, 0, DateTimeKind.Utc));
        var uow = BuildUow([request], new() { [request.Id] = lastReturn });
        var penaltyRepo = new FakePenaltyRepository(PenaltyConstants.AutomaticPenalties.FailureToAmendInTime);
        var appliedPenalties = new FakeAppliedPenaltyRepository();

        var count = await WithdrawalAmendmentPenaltyService.ApplyOverdueAmendmentPenaltiesAsync(
            uow.Object, penaltyRepo, appliedPenalties, NullLogger.Instance, CancellationToken.None);

        Assert.Equal(0, count);
    }
}
