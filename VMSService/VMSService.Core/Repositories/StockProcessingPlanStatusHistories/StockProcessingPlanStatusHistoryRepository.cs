using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlanStatusHistories
{
    public class StockProcessingPlanStatusHistoryRepository(VMSServiceDbContext _db) : IStockProcessingPlanStatusHistoryRepository
    {
        public async Task<bool> Create(StockProcessingPlanStatusHistory model)
        {
            _db.StockProcessingPlanStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingPlanStatusHistory>> FindAll()
        {
            return await _db.StockProcessingPlanStatusHistory
                .Where(x => x.IsDeleted == false)
                .Include(x => x.StockProcessingPlan)
                .ToListAsync();
        }

        public async Task<StockProcessingPlanStatusHistory> FindById(int id)
        {
            return await _db.StockProcessingPlanStatusHistory
                .Include(x => x.StockProcessingPlan)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingPlanStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingPlanStatusHistory model)
        {
            _db.StockProcessingPlanStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingPlanStatusHistory>> FindByStockProcessingPlanId(int stockProcessingPlanId)
        {
            return await _db.StockProcessingPlanStatusHistory
                .Where(x => x.StockProcessingPlanId == stockProcessingPlanId && x.IsDeleted == false)
                .Include(x => x.StockProcessingPlan)
                .ToListAsync();
        }
    }
}
