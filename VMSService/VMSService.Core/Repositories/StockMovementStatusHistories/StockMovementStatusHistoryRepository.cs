using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockMovementStatusHistories
{
    public class StockMovementStatusHistoryRepository(VMSServiceDbContext _db) : IStockMovementStatusHistoryRepository
    {
        public async Task<bool> Create(StockMovementStatusHistory model)
        {
            _db.StockMovementStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockMovementStatusHistory>> FindAll()
        {
            return await _db.StockMovementStatusHistory.Where(x => x.IsDeleted == false)
                .Include(x => x.StockMovement)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .ToListAsync();
        }

        public async Task<StockMovementStatusHistory> FindById(int id)
        {
            return await _db.StockMovementStatusHistory
                .Include(x => x.StockMovement)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockMovementStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockMovementStatusHistory model)
        {
            _db.StockMovementStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<StockMovementStatusHistory>> FindByStockMovementId(int stockMovementId)
        {
            return await _db.StockMovementStatusHistory
                .Where(x => x.StockMovementId == stockMovementId && x.IsDeleted == false)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .ToListAsync();
        }
    }
}
