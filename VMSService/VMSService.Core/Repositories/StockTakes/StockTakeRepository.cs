using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakes
{
    public class StockTakeRepository(VMSServiceDbContext _db) : IStockTakeRepository
    {
        public async Task<bool> Create(StockTake model)
        {
            await _db.StockTakes.AddAsync(model);
            return await Save();
        }

        public async Task<ICollection<StockTake>> FindAll()
        {
            return await _db.StockTakes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.StockTakeDetails)
                .OrderByDescending(x => x.CountDate)
                .ToListAsync();
        }

        public async Task<StockTake> FindById(int id)
        {
            return await _db.StockTakes
                .Include(x => x.StockTakeDetails)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockTakes.AnyAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<bool> Update(StockTake model)
        {
            _db.StockTakes.Update(model);
            return await Save();
        }

        public async Task<StockTake?> FindAmendmentOf(int stockTakeId)
        {
            return await _db.StockTakes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AmendsStockTakeId == stockTakeId && !x.IsDeleted);
        }

        public async Task<int> CountByBranchAndDateAsync(int branchId, DateTime countDate, int? excludeId = null)
        {
            return await _db.StockTakes
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.BranchId == branchId && x.CountDate.Date == countDate.Date
                    && (excludeId == null || x.Id != excludeId))
                .CountAsync();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
