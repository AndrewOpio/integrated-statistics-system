using Microsoft.EntityFrameworkCore;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 06: releasing BOU-origin denomination rows on BM Reject / the ABM's ReturnForRevision
/// (and, pending confirmation, CompleteRecall's approved path) all reuse
/// <see cref="WithdrawalRequestDenominationRepository.DeleteBouRowsWithChildrenAsync"/> — the same
/// method <c>SaveBankDenominations</c> already relied on. These tests cover that method's own
/// correctness (BOU-only, including child box rows; CB-origin left alone) against a real EF Core
/// InMemory database, independent of which caller triggers it.
/// </summary>
public class WithdrawalRequestDenominationRepositoryDeleteTests
{
    private static VMSServiceDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<VMSServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VMSServiceDbContext(options, httpContextAccessor: null);
    }

    [Fact]
    public async Task DeleteBouRowsWithChildrenAsync_Removes_Bou_Rows_And_Their_Dispatched_Boxes()
    {
        using var db = NewContext();
        var requestId = Guid.NewGuid();

        var bouRow = new WithdrawalRequestDenomination
        {
            WithdrawalRequestId = requestId,
            DenominationId = 1,
            DenominationValue = 50_000m,
            Origin = WithdrawalOrigin.Bou,
            DispatchedBoxes =
            [
                new WithdrawalRequestDenominationBox
                {
                    CaseNumber = "101",
                    BundlesTaken = 50,
                    SerialStartTaken = 1,
                    SerialEndTaken = 50_000,
                },
            ],
        };
        db.WithdrawalRequestDenominations.Add(bouRow);
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestDenominationRepository(db);
        await repo.DeleteBouRowsWithChildrenAsync(requestId);
        await db.SaveChangesAsync();

        Assert.Empty(db.WithdrawalRequestDenominations.Where(d => d.WithdrawalRequestId == requestId));
        Assert.Empty(db.WithdrawalRequestDenominationBoxes);
    }

    [Fact]
    public async Task DeleteBouRowsWithChildrenAsync_Leaves_Cb_Origin_Rows_Untouched()
    {
        using var db = NewContext();
        var requestId = Guid.NewGuid();

        var cbRow = new WithdrawalRequestDenomination
        {
            WithdrawalRequestId = requestId,
            DenominationId = 2,
            DenominationValue = 20_000m,
            Origin = WithdrawalOrigin.Cb,
        };
        var bouRow = new WithdrawalRequestDenomination
        {
            WithdrawalRequestId = requestId,
            DenominationId = 1,
            DenominationValue = 50_000m,
            Origin = WithdrawalOrigin.Bou,
        };
        db.WithdrawalRequestDenominations.AddRange(cbRow, bouRow);
        await db.SaveChangesAsync();

        var repo = new WithdrawalRequestDenominationRepository(db);
        await repo.DeleteBouRowsWithChildrenAsync(requestId);
        await db.SaveChangesAsync();

        var remaining = Assert.Single(db.WithdrawalRequestDenominations.Where(d => d.WithdrawalRequestId == requestId));
        Assert.Equal(cbRow.Id, remaining.Id);
        Assert.Equal(WithdrawalOrigin.Cb, remaining.Origin);
    }
}
