using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Penalties;
using VMSService.Data.Entities;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Lightweight in-memory doubles for the automatic-penalty FindByName/Apply pattern —
/// faithful to <see cref="AppliedPenaltyRepository"/>'s real upsert-by-(sourceId, penaltyId)
/// semantics without needing a real DbContext. Shared across the penalty-wiring test files so each
/// one doesn't reinvent the same fake.
/// </summary>
public class FakePenaltyRepository : IPenaltyRepository
{
    private readonly List<Penalty> _penalties;

    public FakePenaltyRepository(params string[] names)
        => _penalties = names.Select((n, i) => new Penalty { Id = i + 1, Name = n, AppliesTo = "Test" }).ToList();

    public Task<ICollection<Penalty>> FindAll() => Task.FromResult<ICollection<Penalty>>(_penalties);
    public Task<Penalty> FindById(int id) => Task.FromResult(_penalties.First(p => p.Id == id));
    public Task<bool> Create(Penalty entity) => throw new NotSupportedException();
    public Task<bool> Update(Penalty entity) => throw new NotSupportedException();
    public Task<bool> IsExists(int id) => Task.FromResult(_penalties.Any(p => p.Id == id));
    public Task<bool> Save() => Task.FromResult(true);
    public Task<ICollection<Penalty>> FindByActive(bool active) => throw new NotSupportedException();
    public Task<ICollection<Penalty>> FindApplicableFor(string appliesTo) => throw new NotSupportedException();

    public Task<Penalty?> FindByName(string name)
        => Task.FromResult(_penalties.FirstOrDefault(p => p.Name == name));

    public decimal Compute(Penalty penalty, decimal quantity = 1, decimal denomValue = 0,
        decimal baseValue = 0, decimal correctlyClassifiedValue = 0) => 0m;
}

public class FakeAppliedPenaltyRepository : IAppliedPenaltyRepository
{
    public List<AppliedPenalty> Rows { get; } = [];

    public Task<ICollection<AppliedPenalty>> FindAll()
        => Task.FromResult<ICollection<AppliedPenalty>>(Rows.Where(a => !a.IsDeleted).ToList());

    public Task<ICollection<AppliedPenalty>> FindBySourceId(string sourceId)
        => Task.FromResult<ICollection<AppliedPenalty>>(
            Rows.Where(a => a.SourceId == sourceId && !a.IsDeleted).ToList());

    public Task<bool> ExistsForSource(string sourceId, int penaltyId)
        => Task.FromResult(Rows.Any(a => a.SourceId == sourceId && a.PenaltyId == penaltyId));

    public Task<ICollection<AppliedPenalty>> FindUnsettled()
        => Task.FromResult<ICollection<AppliedPenalty>>(Rows.Where(a => !a.IsSettledOnRtgs).ToList());

    public Task<AppliedPenalty> FindById(int id) => Task.FromResult(Rows.First(a => a.Id == id));
    public Task<bool> Create(AppliedPenalty entity) => throw new NotSupportedException();
    public Task<bool> Update(AppliedPenalty entity) => throw new NotSupportedException();
    public Task<bool> IsExists(int id) => Task.FromResult(Rows.Any(a => a.Id == id));
    public Task<bool> Save() => Task.FromResult(true);

    public Task<AppliedPenalty> Apply(string source, string sourceId, int penaltyId, int? quantity = null,
        decimal? denomValue = null, decimal? baseValue = null, decimal? correctlyClassifiedValue = null,
        string? description = null)
    {
        // Deliberately not filtered by IsDeleted, matching the real repository — a removed/waived
        // row is reused (un-deleted) rather than duplicated.
        var existing = Rows.FirstOrDefault(a => a.SourceId == sourceId && a.PenaltyId == penaltyId);
        if (existing is not null)
        {
            existing.IsDeleted = false;
            if (description is not null) existing.Description = description;
            return Task.FromResult(existing);
        }

        var applied = new AppliedPenalty
        {
            Id = Rows.Count + 1,
            Source = source,
            SourceId = sourceId,
            PenaltyId = penaltyId,
            Description = description ?? string.Empty,
        };
        Rows.Add(applied);
        return Task.FromResult(applied);
    }

    public Task Remove(int appliedPenaltyId)
    {
        var existing = Rows.FirstOrDefault(a => a.Id == appliedPenaltyId);
        if (existing is not null) existing.IsDeleted = true;
        return Task.CompletedTask;
    }

    public Task Waive(int appliedPenaltyId, string reason, string waivedBy)
    {
        var existing = Rows.FirstOrDefault(a => a.Id == appliedPenaltyId && !a.IsDeleted);
        if (existing is not null)
        {
            existing.ToBeWaived = true;
            existing.WaiverReason = reason;
            existing.WaivedBy = waivedBy;
            existing.WaivedDate = DateTime.Now;
        }
        return Task.CompletedTask;
    }
}
