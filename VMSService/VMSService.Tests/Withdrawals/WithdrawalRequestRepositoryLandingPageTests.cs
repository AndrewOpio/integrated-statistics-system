using Microsoft.EntityFrameworkCore;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 19: the CB and BOU landing pages must query only what they display (filtered,
/// terminal-and-past-date requests excluded into History) instead of loading every request
/// unfiltered. These tests exercise the repository-level query logic directly against an
/// EF Core InMemory database — fast, self-contained, no LocalDB dependency.
/// </summary>
public class WithdrawalRequestRepositoryLandingPageTests
{
    private static VMSServiceDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<VMSServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VMSServiceDbContext(options, httpContextAccessor: null);
    }

    private static WithdrawalRequest NewRequest(
        int cbId = 1,
        string statusName = WithdrawalStatusNames.PendingInternalApproval,
        DateOnly? collectionDate = null,
        bool isDeleted = false,
        string bouBranchCode = "KLA",
        decimal totalAmount = 10_000_000m)
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = cbId,
            StatusTypeId = 1,
            StatusTypeName = statusName,
            CollectionDate = collectionDate ?? DateOnly.FromDateTime(DateTime.Today),
            IsDeleted = isDeleted,
            BouBranchCode = bouBranchCode,
            TotalAmount = totalAmount,
        };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private static readonly DateOnly LastWeek = Today.AddDays(-7);
    private static readonly DateOnly ThirteenMonthsAgo = Today.AddMonths(-13);
    private static readonly DateOnly TwelveMonthsAgo = Today.AddMonths(-12);

    // ── GetActiveByCbAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveByCbAsync_Excludes_Requests_For_Other_CBs()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(cbId: 1));
        db.WithdrawalRequests.Add(NewRequest(cbId: 2));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        var r = Assert.Single(result);
        Assert.Equal(1, r.CbId);
    }

    [Fact]
    public async Task GetActiveByCbAsync_Excludes_Deleted_Requests()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(isDeleted: true));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveByCbAsync_Excludes_Terminal_Request_With_Past_Collection_Date()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveByCbAsync_Includes_Terminal_Request_With_Todays_Collection_Date()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: Today));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Single(result);
    }

    [Theory]
    [InlineData(WithdrawalStatusNames.RejectComplete)]
    [InlineData(WithdrawalStatusNames.RejectExceptionalApprovalComplete)]
    [InlineData(WithdrawalStatusNames.RecallComplete)]
    [InlineData(WithdrawalStatusNames.Expired)]
    public async Task GetActiveByCbAsync_Excludes_Every_Terminal_Status_When_Past_Date(string terminalStatus)
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: terminalStatus, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(WithdrawalStatusNames.Rejected)]
    [InlineData(WithdrawalStatusNames.RejectExceptionalApproval)]
    [InlineData(WithdrawalStatusNames.RecallApproved)]
    public async Task GetActiveByCbAsync_Keeps_Decided_But_Not_Yet_Completed_Statuses_Visible_Even_When_Past_Date(string awaitingCompletionStatus)
    {
        // Rejected/Exceptional Approval Rejected/Recall Approved still owe a ReturnRtgsPending or
        // RecallRtgsPending completion step — hiding them once their (already-past) collection
        // date ages out would hide real work from Back Office.
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: awaitingCompletionStatus, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveByCbAsync_Includes_NonTerminal_Request_Regardless_Of_Collection_Date()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.PendingInternalApproval, collectionDate: ThirteenMonthsAgo));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveByCbAsync(cbId: 1, today: Today);

        Assert.Single(result);
    }

    // ── GetHistoryByCbAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryByCbAsync_Returns_Terminal_Request_With_Past_Collection_Date()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryByCbAsync(cbId: 1, today: Today, since: TwelveMonthsAgo);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetHistoryByCbAsync_Excludes_Terminal_Request_With_Todays_Collection_Date()
    {
        // Not stale yet — still belongs on the active main list, not History.
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: Today));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryByCbAsync(cbId: 1, today: Today, since: TwelveMonthsAgo);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryByCbAsync_Excludes_NonTerminal_Request_Even_With_Past_Collection_Date()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Rejected, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryByCbAsync(cbId: 1, today: Today, since: TwelveMonthsAgo);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryByCbAsync_Excludes_Requests_Older_Than_The_Lookback_Window()
    {
        // "Older than that, the CB asks BOU directly" — the 12-month cutoff is a hard floor.
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: ThirteenMonthsAgo));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryByCbAsync(cbId: 1, today: Today, since: TwelveMonthsAgo);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryByCbAsync_Excludes_Deleted_And_Other_CBs()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(cbId: 1, statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek, isDeleted: true));
        db.WithdrawalRequests.Add(NewRequest(cbId: 2, statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryByCbAsync(cbId: 1, today: Today, since: TwelveMonthsAgo);

        Assert.Empty(result);
    }

    // ── GetActiveAsync / GetHistoryAsync (BOU side) ─────────────────────────

    [Fact]
    public async Task GetActiveAsync_Excludes_Terminal_Past_Requests_Same_As_CB_Side()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.RecallComplete, collectionDate: LastWeek));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.PendingVerification, collectionDate: LastWeek));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveAsync(today: Today);

        var r = Assert.Single(result);
        Assert.Equal(WithdrawalStatusNames.PendingVerification, r.StatusTypeName);
    }

    [Fact]
    public async Task GetActiveAsync_With_No_Branch_Returns_Every_Branch()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(bouBranchCode: "KLA"));
        db.WithdrawalRequests.Add(NewRequest(bouBranchCode: "MBL"));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveAsync(today: Today, bouBranchCode: null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetActiveAsync_With_Branch_Filters_To_That_Branch_Only()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(bouBranchCode: "KLA"));
        db.WithdrawalRequests.Add(NewRequest(bouBranchCode: "MBL"));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetActiveAsync(today: Today, bouBranchCode: "KLA");

        var r = Assert.Single(result);
        Assert.Equal("KLA", r.BouBranchCode);
    }

    [Fact]
    public async Task GetHistoryAsync_Mirrors_CB_Side_Rules_And_Respects_Branch_Filter()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek, bouBranchCode: "KLA"));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: LastWeek, bouBranchCode: "MBL"));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: Today, bouBranchCode: "KLA"));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var result = await repo.GetHistoryAsync(today: Today, since: TwelveMonthsAgo, bouBranchCode: "KLA");

        var r = Assert.Single(result);
        Assert.Equal("KLA", r.BouBranchCode);
        Assert.Equal(LastWeek, r.CollectionDate);
    }

    // ── SumApprovedAmountAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SumApprovedAmountAsync_Includes_Old_Approved_Requests_Regardless_Of_Active_History_Split()
    {
        // Regression check for the pre-existing bug: Total Processed was always 0 because it was
        // computed from a collection that had already excluded Approved rows. This must be a true
        // lifetime sum, unaffected by the terminal-and-past exclusion used for the row list.
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: ThirteenMonthsAgo, totalAmount: 15_000_000m));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, collectionDate: Today, totalAmount: 25_000_000m));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.PendingApproval, collectionDate: Today, totalAmount: 99_000_000m));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var total = await repo.SumApprovedAmountAsync();

        Assert.Equal(40_000_000m, total);
    }

    [Fact]
    public async Task SumApprovedAmountAsync_Excludes_Deleted_Requests()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, totalAmount: 15_000_000m, isDeleted: true));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var total = await repo.SumApprovedAmountAsync();

        Assert.Equal(0m, total);
    }

    [Fact]
    public async Task SumApprovedAmountAsync_Scopes_To_Branch_When_Given()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, totalAmount: 15_000_000m, bouBranchCode: "KLA"));
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.Approved, totalAmount: 30_000_000m, bouBranchCode: "MBL"));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var total = await repo.SumApprovedAmountAsync(bouBranchCode: "KLA");

        Assert.Equal(15_000_000m, total);
    }

    [Fact]
    public async Task SumApprovedAmountAsync_Returns_Zero_When_Nothing_Approved()
    {
        using var db = NewContext();
        db.WithdrawalRequests.Add(NewRequest(statusName: WithdrawalStatusNames.PendingApproval));
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestRepository(db);
        var total = await repo.SumApprovedAmountAsync();

        Assert.Equal(0m, total);
    }

    // ── IsStalePreSubmission ─────────────────────────────────────────────────

    [Theory]
    [InlineData(WithdrawalStatusNames.Draft)]
    [InlineData(WithdrawalStatusNames.PendingInternalApproval)]
    [InlineData(WithdrawalStatusNames.ReturnedForRevision)]
    public void IsStalePreSubmission_True_For_PreSubmission_Status_With_Past_Date(string status)
    {
        Assert.True(WithdrawalRequestRepository.IsStalePreSubmission(status, Yesterday, Today));
    }

    [Theory]
    [InlineData(WithdrawalStatusNames.Draft)]
    [InlineData(WithdrawalStatusNames.PendingInternalApproval)]
    [InlineData(WithdrawalStatusNames.ReturnedForRevision)]
    public void IsStalePreSubmission_False_For_PreSubmission_Status_With_Todays_Date(string status)
    {
        Assert.False(WithdrawalRequestRepository.IsStalePreSubmission(status, Today, Today));
    }

    [Fact]
    public void IsStalePreSubmission_False_For_PreSubmission_Status_With_Future_Date()
    {
        Assert.False(WithdrawalRequestRepository.IsStalePreSubmission(WithdrawalStatusNames.Draft, Today.AddDays(1), Today));
    }

    [Fact]
    public void IsStalePreSubmission_False_Once_Request_Has_Left_PreSubmission_Even_With_Past_Date()
    {
        // Once submitted onward to BOU, Section 10's rule no longer applies — a different
        // mechanism (auto-expiry, or BOU's own workflow) governs from here.
        Assert.False(WithdrawalRequestRepository.IsStalePreSubmission(WithdrawalStatusNames.PendingRTGSVerification, Yesterday, Today));
    }
}
