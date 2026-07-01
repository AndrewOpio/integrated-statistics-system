using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingSessionStatusHistories
{
    public class StockProcessingSessionStatusHistoryRepository(VMSServiceDbContext _db) : IStockProcessingSessionStatusHistoryRepository
    {
        public async Task<bool> Create(StockProcessingSessionStatusHistory model)
        {
            _db.StockProcessingSessionStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingSessionStatusHistory>> FindAll()
        {
            return await _db.StockProcessingSessionStatusHistory
                .Where(x => x.IsDeleted == false)
                .Include(x => x.StockProcessingSession)
                .ToListAsync();
        }

        public async Task<StockProcessingSessionStatusHistory> FindById(int id)
        {
            return await _db.StockProcessingSessionStatusHistory
                .Include(x => x.StockProcessingSession)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingSessionStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingSessionStatusHistory model)
        {
            _db.StockProcessingSessionStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingSessionStatusHistory>> FindByStockProcessingSessionId(int stockProcessingSessionId)
        {
            return await _db.StockProcessingSessionStatusHistory
                .Where(x => x.StockProcessingSessionId == stockProcessingSessionId && x.IsDeleted == false)
                .Include(x => x.StockProcessingSession)
                .ToListAsync();
        }
    }
}
