using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ReceivedStockStatusHistories
{
    public class ReceivedStockStatusHistoryRepository(VMSServiceDbContext _db) : IReceivedStockStatusHistoryRepository
    {
        public async Task<bool> Create(ReceivedStockStatusHistory model)
        {
            _db.ReceivedStockStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<ReceivedStockStatusHistory>> FindAll()
        {
            return await _db.ReceivedStockStatusHistory.Where(x => x.IsDeleted == false)
                .Include(x => x.ReceivedStock)
                .ToListAsync();
        }

        public async Task<ReceivedStockStatusHistory> FindById(int id)
        {
            return await _db.ReceivedStockStatusHistory
                .Include(x => x.ReceivedStock)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ReceivedStockStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ReceivedStockStatusHistory model)
        {
            _db.ReceivedStockStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<ReceivedStockStatusHistory>> FindByReceivedStockId(int receivedStockId)
        {
            return await _db.ReceivedStockStatusHistory
                .Where(x => x.ReceivedStockId == receivedStockId)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }
    }
}