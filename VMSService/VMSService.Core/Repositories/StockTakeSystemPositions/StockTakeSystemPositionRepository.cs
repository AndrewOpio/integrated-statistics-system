using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakeSystemPositions
{
    public class StockTakeSystemPositionRepository(VMSServiceDbContext _db) : IStockTakeSystemPositionRepository
    {
        public async Task<bool> CreateRange(ICollection<StockTakeSystemPosition> models)
        {
            await _db.StockTakeSystemPositions.AddRangeAsync(models);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<ICollection<StockTakeSystemPosition>> FindByStockTakeId(int stockTakeId)
        {
            return await _db.StockTakeSystemPositions
                .AsNoTracking()
                .Where(x => x.StockTakeId == stockTakeId)
                .ToListAsync();
        }

        public async Task<bool> DeleteByStockTakeId(int stockTakeId)
        {
            var existing = await _db.StockTakeSystemPositions
                .Where(x => x.StockTakeId == stockTakeId)
                .ToListAsync();
            if (existing.Count == 0) return true;

            _db.StockTakeSystemPositions.RemoveRange(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
