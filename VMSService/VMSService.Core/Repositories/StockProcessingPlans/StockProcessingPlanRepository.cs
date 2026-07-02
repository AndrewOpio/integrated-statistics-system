using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlans
{
    public class StockProcessingPlanRepository(VMSServiceDbContext _db) : IStockProcessingPlanRepository
    {
        public async Task<bool> Create(StockProcessingPlan model)
        {
            _db.StockProcessingPlans.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingPlan>> FindAll()
        {
            return await _db.StockProcessingPlans
                .Where(x => !x.IsDeleted)
                .Include(x => x.StockMovement)
                .ToListAsync();
        }

        public async Task<StockProcessingPlan> FindById(int id)
        {
            return await _db.StockProcessingPlans
                .Include(x => x.StockMovement)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingPlans
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingPlan model)
        {
            _db.StockProcessingPlans.Update(model);
            return await Save();
        }
    }
}
