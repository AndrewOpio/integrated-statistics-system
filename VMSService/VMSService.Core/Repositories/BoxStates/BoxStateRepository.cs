using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoxStates;
public class BoxStateRepository(VMSServiceDbContext _db) : IBoxStateRepository
{
    public async Task<BoxState?> FindByBoxAndDenominationAsync(string caseNumber, int denominationId, CancellationToken ct = default)
        => await _db.BoxStates
        .FirstOrDefaultAsync(b => b.CaseNumber == caseNumber && b.DenominationId == denominationId, ct);

    public async Task<(BoxState? State, string SerialPrefix)> FindByBoxAndDenominationWithPrefixAsync(
    string caseNumber, int denominationId, CancellationToken ct)
    {
        var result = await (from bs in _db.BoxStates
                            join pd in _db.PackingListDetails on bs.PackingListDetailsId equals pd.Id
                            join pr in _db.PackingListRanges on pd.PackingListRangeId equals pr.Id
                            where bs.CaseNumber == caseNumber && bs.DenominationId == denominationId
                            select new { State = bs, pr.SNPrefix })
                            .FirstOrDefaultAsync(ct);

        return (result?.State, result?.SNPrefix ?? "");
    }

    public async Task<List<BoxState>> FindManyForReversalAsync(IEnumerable<(string CaseNumber, int DenominationId)> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        if (keyList.Count == 0) return new List<BoxState>();

        var boxNos = keyList.Select(k => k.CaseNumber).Distinct().ToList();
        var matchingDenoms = keyList.Select(k => k.DenominationId).Distinct().ToList();

        var candidates = await _db.BoxStates
            .Where(b => boxNos.Contains(b.CaseNumber) && matchingDenoms.Contains(b.DenominationId))
            .ToListAsync(ct);

        // Final precise match against the tuple set
        var keySet = keyList.ToHashSet();
        return candidates
            .Where(b => keySet.Contains((b.CaseNumber, b.DenominationId)))
            .ToList();
    }
    public async Task<List<BoxState>> FindForRequestAsync(Guid requestId, CancellationToken ct = default)
        => await _db.BoxStates
        .Where(b => b.LastDispatchedRequestId == requestId)
        .ToListAsync(ct);
}
