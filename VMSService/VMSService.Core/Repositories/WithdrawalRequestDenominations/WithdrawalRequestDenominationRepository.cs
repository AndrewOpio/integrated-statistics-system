using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.BoxStates;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Repositories.WithdrawalRequestDenominations
{
    //public record HeldByBoxResult(string CaseNumber, int DenominationId, int BundlesHeld, long MinSerialStartHeld);
    public class WithdrawalRequestDenominationRepository(VMSServiceDbContext _db) : IWithdrawalRequestDenominationRepository
    {
        public async Task<ICollection<WithdrawalRequestDenomination>> FindAll()
            => await _db.WithdrawalRequestDenominations.ToListAsync();

        public async Task<WithdrawalRequestDenomination> FindById(Guid id)
        => await _db.WithdrawalRequestDenominations.FindAsync(id)
           ?? throw new KeyNotFoundException($"Withdrawal Request Denomination {id} not found.");

        public async Task<bool> Create(WithdrawalRequestDenomination entity)
        {
            await _db.WithdrawalRequestDenominations.AddAsync(entity);
            return true;
        }
        public Task<bool> Update(WithdrawalRequestDenomination entity)
        {
            _db.WithdrawalRequestDenominations.Update(entity);
            return Task.FromResult(true);
        }
        public async Task<bool> IsExists(Guid id)
        => await _db.WithdrawalRequestDenominations.AnyAsync(d => d.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAsync(
        Guid requestId, CancellationToken ct = default)
        => await _db.WithdrawalRequestDenominations
            .Where(d => d.WithdrawalRequestId == requestId)
            .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAndOriginAsync(
        Guid requestId, string origin, CancellationToken ct = default)
        => await _db.WithdrawalRequestDenominations
            .Where(d => d.WithdrawalRequestId == requestId && d.Origin == origin)
            .ToListAsync(ct);

        public void DeleteByRequest(Guid requestId, string origin)
        => _db.WithdrawalRequestDenominations.RemoveRange(
            _db.WithdrawalRequestDenominations
              .Where(d => d.WithdrawalRequestId == requestId && d.Origin == origin));

        //public async Task CopyToBouAsync(Guid requestId, string userId, CancellationToken ct)
        //{
        //    var cbLines = await _db.WithdrawalRequestDenominations
        //        .Where(d => d.WithdrawalRequestId == requestId
        //                 && d.Origin == WithdrawalOrigin.Cb)
        //        .ToListAsync(ct);

        //    foreach (var line in cbLines)
        //    {
        //        await _db.WithdrawalRequestDenominations.AddAsync(new WithdrawalRequestDenomination
        //        {
        //            WithdrawalRequestId = requestId,
        //            DenominationId = line.DenominationId,
        //            DenominationTypeId = line.DenominationTypeId,
        //            Bundles = line.Bundles,
        //            Boxes = line.Boxes,
        //            Amount = line.Amount,
        //            Origin = WithdrawalOrigin.Bou,
        //            CreatedBy = userId,
        //            CreatedDate = DateTime.Now,
        //        }, ct);
        //    }
        //}

        public async Task<bool> HasBouOriginAsync(Guid requestId, CancellationToken ct = default)
            => await _db.WithdrawalRequestDenominations
            .AnyAsync(d => d.WithdrawalRequestId == requestId && d.Origin == WithdrawalOrigin.Bou, ct);

        public async Task<HashSet<Guid>> GetRequestIdsWithBouOriginAsync(IEnumerable<Guid> requestIds, CancellationToken ct = default)
        {
            var ids = requestIds.ToList();
            if (ids.Count == 0) return new HashSet<Guid>();

            return (await _db.WithdrawalRequestDenominations
                .Where(d => ids.Contains(d.WithdrawalRequestId) && d.Origin == WithdrawalOrigin.Bou)
                .Select(d => d.WithdrawalRequestId)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet();
        }

        public async Task DeleteBouRowsWithChildrenAsync(Guid requestId, CancellationToken ct = default)
        {
            var bankRows = await _db.WithdrawalRequestDenominations
                .Include(d => d.DispatchedBoxes)
                .Where(d => d.WithdrawalRequestId == requestId && d.Origin == WithdrawalOrigin.Bou)
                .ToListAsync(ct);

            if (bankRows.Count == 0) return;

            var boxes = bankRows.SelectMany(r => r.DispatchedBoxes).ToList();
            if (boxes.Count > 0)
                _db.WithdrawalRequestDenominationBoxes.RemoveRange(boxes);

            _db.WithdrawalRequestDenominations.RemoveRange(bankRows);
        }

        public async Task<List<AvailableBoxDto>> GetAvailableBoxesAsync(
            int denominationId, int branchId, Guid? excludeRequestId, CancellationToken ct = default)
        {
            // ── Step 1: Pull dispatchable boxes from BoxState 
            var boxes = await (from bs in _db.BoxStates
                               join pd in _db.PackingListDetails on bs.PackingListDetailsId equals pd.Id
                               join pr in _db.PackingListRanges on pd.PackingListRangeId equals pr.Id
                               where bs.DenominationId == denominationId
                               && bs.BranchIsAtId == branchId
                               && bs.IsAvailable
                               && bs.RemainingBundles > 0
                               select new
                               {
                                   bs.CaseNumber,
                                   bs.CurrentSerialCursor,
                                   bs.RemainingBundles,
                                   pr.SNPrefix, 
                                   OriginalSerialEnd = pd.SerialNoteEndNumber,
                               })
                               .ToListAsync(ct);

            // ── Step 2: If editing, restore capacity for boxes the current request is holding ──
            if (excludeRequestId.HasValue)
            {
                var heldByRequest = await _db.WithdrawalRequestDenominationBoxes
                    .Where(b => b.WithdrawalRequestDenomination.WithdrawalRequestId == excludeRequestId.Value
                             && b.WithdrawalRequestDenomination.DenominationId == denominationId)
                    .GroupBy(b => b.CaseNumber)
                    .Select(g => new
                    {
                        CaseNumber = g.Key,
                        BundlesHeld = g.Sum(x => x.BundlesTaken),
                        MinSerialStartHeld = g.Min(x => x.SerialStartTaken),
                    })
                    .ToListAsync(ct);

                if (heldByRequest.Count > 0)
                {

                    var heldLookup = heldByRequest.ToDictionary(h => h.CaseNumber);

                    var restoredList = new List<dynamic>();
                    var seenBoxes = new HashSet<string>();

                    foreach (var b in boxes)
                    {
                        seenBoxes.Add(b.CaseNumber);
                        if (heldLookup.TryGetValue(b.CaseNumber, out var held))
                        {

                            restoredList.Add(new
                            {
                                b.CaseNumber,
                                CurrentSerialCursor = held.MinSerialStartHeld,
                                RemainingBundles = b.RemainingBundles + held.BundlesHeld,
                                b.SNPrefix,
                                b.OriginalSerialEnd,
                            });
                        }
                        else
                        {
                            restoredList.Add(b);
                        }
                    }

                    // Boxes the current request fully exhausted (RemainingBundles = 0 in BoxState,
                    // so they were filtered out of the initial query). Re-fetch and add back.
                    var missingBoxes = heldByRequest
                        .Where(h => !seenBoxes.Contains(h.CaseNumber))
                        .Select(h => h.CaseNumber)
                        .ToList();

                    if (missingBoxes.Count > 0)
                    {
                        var missingDetails = await (
                            from bs in _db.BoxStates
                            join pd in _db.PackingListDetails on bs.PackingListDetailsId equals pd.Id
                            join pr in _db.PackingListRanges on pd.PackingListRangeId equals pr.Id
                            where missingBoxes.Contains(bs.CaseNumber)
                               && bs.DenominationId == denominationId
                               && bs.BranchIsAtId == branchId
                               && bs.IsAvailable
                            select new
                            {
                                bs.CaseNumber,
                                bs.CurrentSerialCursor,
                                bs.RemainingBundles,
                                pr.SNPrefix,
                                OriginalSerialEnd = pd.SerialNoteEndNumber,
                            })
                            .ToListAsync(ct);

                        foreach (var b in missingDetails)
                        {
                            var held = heldLookup[b.CaseNumber];
                            restoredList.Add(new
                            {
                                b.CaseNumber,
                                CurrentSerialCursor = held.MinSerialStartHeld,
                                RemainingBundles = held.BundlesHeld,
                                b.SNPrefix,
                                b.OriginalSerialEnd,
                            });
                        }
                    }

                    boxes = restoredList.Cast<dynamic>().Select(d => new
                    {
                        CaseNumber = (string)d.CaseNumber,
                        CurrentSerialCursor = (long)d.CurrentSerialCursor,
                        RemainingBundles = (int)d.RemainingBundles,
                        SNPrefix = (string)d.SNPrefix,
                        OriginalSerialEnd = (int)d.OriginalSerialEnd,
                    }).ToList();
                }
            }

            // ── Step 3: Project to DTO, sort by box number ascending (oldest first) ──
            return boxes
                .OrderBy(b => int.TryParse(b.CaseNumber, out var n) ? n : int.MaxValue)
                .Select(b => new AvailableBoxDto
                {
                    CaseNumber = b.CaseNumber,
                    SerialStart = b.SNPrefix + b.CurrentSerialCursor.ToString(),
                    SerialEnd = b.SNPrefix + b.OriginalSerialEnd.ToString(),
                    RemainingBundles = b.RemainingBundles,
                })
                .ToList();

        }

        public async Task<List<HeldByBoxResult>> GetHeldByBoxAsync(Guid requestId, CancellationToken ct = default)
            => await (
                from b in _db.WithdrawalRequestDenominationBoxes
                join d in _db.WithdrawalRequestDenominations on b.WithdrawalRequestDenominationId equals d.Id
                where d.WithdrawalRequestId == requestId
                group new { b, d } by new { b.CaseNumber, d.DenominationId } into g
                select new HeldByBoxResult(
                    g.Key.CaseNumber,
                    g.Key.DenominationId,
                    g.Sum(x => x.b.BundlesTaken),
                    g.Min(x => x.b.SerialStartTaken)
                ))
                .ToListAsync(ct);


    }
}
