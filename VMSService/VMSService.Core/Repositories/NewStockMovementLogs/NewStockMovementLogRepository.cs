using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.NewStockMovementLogs
{
    public class NewStockMovementLogRepository(VMSServiceDbContext _db) : INewStockMovementLogRepository
    {
        public async Task<NewStockMovementlog> CreateAsync(NewStockMovementlog entity)
        {
            await _db.NewStockMovementLogs.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> CreateBatchAsync(List<NewStockMovementlog> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            try
            {
                await _db.NewStockMovementLogs.AddRangeAsync(entities);
                var saveResult = await _db.SaveChangesAsync();
                return saveResult > 0;
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging
                throw new InvalidOperationException($"Failed to create batch of {entities.Count} NewStockMovementlog entries", ex);
            }
        }

        public async Task<NewStockMovementlog> FindByIdAsync(int id)
        {
            return await _db.NewStockMovementLogs
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<ICollection<NewStockMovementlog>> FindByStockMovementIdAsync(int stockMovementId)
        {
            return await _db.NewStockMovementLogs
                .AsNoTracking()
                .Where(l => l.StockMovementId == stockMovementId)
                .ToListAsync();
        }

        public async Task<bool> SaveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
