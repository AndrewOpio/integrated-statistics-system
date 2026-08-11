using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakeStatusHistories
{
    public class StockTakeStatusHistoryRepository(VMSServiceDbContext _db) : IStockTakeStatusHistoryRepository
    {
        public async Task<bool> Create(StockTakeStatusHistory model)
        {
            await _db.StockTakeStatusHistory.AddAsync(model);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<ICollection<StockTakeStatusHistory>> FindByStockTakeId(int stockTakeId)
        {
            return await _db.StockTakeStatusHistory
                .Where(x => x.StockTakeId == stockTakeId && !x.IsDeleted)
                .ToListAsync();
        }
    }
}
