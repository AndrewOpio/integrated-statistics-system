using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Core.Repository.Common;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionAuthorizations
{
    public class DestructionAuthorizationRepository(VMSServiceDbContext _db,
        ILogger<DestructionAuthorizationRepository> _logger) : IDestructionAuthorizationRepository
    {
        public async Task<bool> Create(DestructionAuthorization entity)
        {
            await _db.DestructionAuthorizations.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<DestructionAuthorization>> FindAll()
        {
            return await _db.DestructionAuthorizations
                .AsNoTracking()
                .Include(a => a.DestructionAuthorizationDenominations)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        public async Task<DestructionAuthorization> FindById(int id)
        {
            var result = await _db.DestructionAuthorizations
                .Include(a => a.DestructionAuthorizationDenominations)
                .FirstOrDefaultAsync(a => a.Id == id);
            return result!;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionAuthorizations.AnyAsync(a => a.Id == id);
        }

        public async Task<bool> Update(DestructionAuthorization entity)
        {
            _db.DestructionAuthorizations.Update(entity);
            return await Save();
        }

        public async Task<bool> UpdateWithChildren(DestructionAuthorization entity)
        {
            var oldDenoms = _db.DestructionAuthorizationDenominations
                .Where(d => d.DestructionAuthorizationId == entity.Id);
            _db.DestructionAuthorizationDenominations.RemoveRange(oldDenoms);

            _db.DestructionAuthorizations.Update(entity);
            return await Save();
        }

        public async Task<ICollection<DestructionAuthorization>> GetApprovedAuthorizations()
        {
            return await _db.DestructionAuthorizations
                .AsNoTracking()
                .Include(a => a.DestructionAuthorizationDenominations)
                .Where(a => a.CurrentStatusName == StatusConstants.DestructionAuthorization.Approved ||
                            a.CurrentStatusName == StatusConstants.DestructionAuthorization.AmendmentApproved)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToListAsync();
        }

        public async Task<Dictionary<int, decimal>> GetRemainingAmounts(int authorizationId, int? excludeRequestId = null)
        {
            var authorization = await _db.DestructionAuthorizations
                .Include(a => a.DestructionAuthorizationDenominations)
                .FirstOrDefaultAsync(a => a.Id == authorizationId);

            if (authorization == null) return new Dictionary<int, decimal>();

            // Sum amounts already used by other requests linked to this authorization
            // (excludeRequestId lets an Edit screen see its own remaining balance without double-counting itself).
            var usedAmounts = await _db.DestructionRequests
                .Where(r => r.DestructionAuthorizationId == authorizationId && r.Id != (excludeRequestId ?? 0))
                .SelectMany(r => r.DestructionRequestDenominations!)
                .GroupBy(d => d.DenominationId)
                .Select(g => new { DenominationId = g.Key, Used = g.Sum(d => d.Amount) })
                .ToListAsync();

            var usedMap = usedAmounts.ToDictionary(x => x.DenominationId, x => x.Used);

            return (authorization.DestructionAuthorizationDenominations ?? [])
                .GroupBy(d => d.DenominationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => d.Amount) - usedMap.GetValueOrDefault(g.Key, 0m));
        }

        public async Task<Dictionary<int, int>> GetRemainingBags(int authorizationId, int? excludeRequestId = null)
        {
            var authorization = await _db.DestructionAuthorizations
                .Include(a => a.DestructionAuthorizationDenominations)
                .FirstOrDefaultAsync(a => a.Id == authorizationId);

            if (authorization == null) return new Dictionary<int, int>();

            // NumberOfBags is stored as a string, so pull the raw rows and parse/sum in memory
            // rather than in the SQL translation.
            var usedBagsRaw = await _db.DestructionRequests
                .Where(r => r.DestructionAuthorizationId == authorizationId && r.Id != (excludeRequestId ?? 0))
                .SelectMany(r => r.DestructionRequestDenominations!)
                .Select(d => new { d.DenominationId, d.NumberOfBags })
                .ToListAsync();

            var usedMap = usedBagsRaw
                .GroupBy(x => x.DenominationId)
                .ToDictionary(g => g.Key, g => g.Sum(x => int.TryParse(x.NumberOfBags, out var n) ? n : 0));

            return (authorization.DestructionAuthorizationDenominations ?? [])
                .GroupBy(d => d.DenominationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(d => int.TryParse(d.NumberOfBags, out var authorized) ? authorized : 0) - usedMap.GetValueOrDefault(g.Key, 0));
        }

        public async Task<DestructionAuthorization?> FindActiveByBranch(int branchId, int? excludeId = null)
        {
            return await _db.DestructionAuthorizations
                .AsNoTracking()
                .Where(a => a.BranchId == branchId
                         && !StatusConstants.DestructionAuthorization.TerminalStatuses.Contains(a.CurrentStatusName)
                         && a.Id != (excludeId ?? 0))
                .FirstOrDefaultAsync();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
