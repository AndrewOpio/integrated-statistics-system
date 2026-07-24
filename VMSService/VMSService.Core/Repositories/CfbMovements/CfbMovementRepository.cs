using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbMovements
{
    public class CfbMovementRepository(VMSServiceDbContext _db) : ICfbMovementRepository
    {
        public async Task<IReadOnlyList<CfbCashMovement>> GetByBoxIdsAsync(IEnumerable<int> boxIds)
            => await _db.CfbCashMovements
                .Where(m => boxIds.Contains(m.CfbBoxId))
                .ToListAsync();

        public async Task<IReadOnlyList<CfbCashMovement>> GetByBoxIdAsync(int boxId)
            => await _db.CfbCashMovements
                .Where(m => m.CfbBoxId == boxId)
                .ToListAsync();

        public async Task<bool> Add(CfbCashMovement entity)
        {
            await _db.CfbCashMovements.AddAsync(entity);
            return true;
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}