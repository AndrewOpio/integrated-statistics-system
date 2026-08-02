using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BagMovementStatusHistories
{
    public class BagMovementStatusHistoryRepository(VMSServiceDbContext _db,
        ILogger<BagMovementStatusHistoryRepository> _logger) : IBagMovementStatusHistoryRepository
    {
        public async Task<bool> Create(BagMovementStatusHistory entity)
        {
            await _db.BagMovementStatusHistory.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BagMovementStatusHistory>> FindAll()
        {
            return await _db.BagMovementStatusHistory.AsNoTracking().ToListAsync();
        }

        public async Task<BagMovementStatusHistory> FindById(int id)
        {
            return (await _db.BagMovementStatusHistory.FirstOrDefaultAsync(r => r.Id == id))!;
        }

        public async Task<ICollection<BagMovementStatusHistory>> FindByBagMovementId(int bagMovementId)
        {
            return await _db.BagMovementStatusHistory
                .AsNoTracking()
                .Where(h => h.BagMovementId == bagMovementId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BagMovementStatusHistory.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(BagMovementStatusHistory entity)
        {
            _db.BagMovementStatusHistory.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
