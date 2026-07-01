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

        public async Task<Dictionary<int, decimal>> GetRemainingAmounts(int authorizationId)
        {
            var authorization = await _db.DestructionAuthorizations
                .Include(a => a.DestructionAuthorizationDenominations)
                .FirstOrDefaultAsync(a => a.Id == authorizationId);

            if (authorization == null) return new Dictionary<int, decimal>();

            // Sum amounts already used in sessions linked to this authorization
            var usedAmounts = await _db.DestructionRequests
                .Where(r => r.DestructionAuthorizationId == authorizationId)
                .SelectMany(r => r.DestructionRequestDenominations!)
                .GroupBy(d => d.DenominationId)
                .Select(g => new { DenominationId = g.Key, Used = g.Sum(d => d.Amount) })
                .ToListAsync();

            var usedMap = usedAmounts.ToDictionary(x => x.DenominationId, x => x.Used);

            return (authorization.DestructionAuthorizationDenominations ?? [])
                .ToDictionary(
                    d => d.DenominationId,
                    d => d.Amount - usedMap.GetValueOrDefault(d.DenominationId, 0m));
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
