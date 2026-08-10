using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingResults
{
    public class StockProcessingResultRepository(VMSServiceDbContext _db) : IStockProcessingResultRepository
    {
        public async Task<bool> Create(StockProcessingResult model)
        {
            _db.StockProcessingResults.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingResult>> FindAll()
        {
            return await _db.StockProcessingResults
                .Where(x => !x.IsDeleted)
                .Include(x => x.StockProcessingSession)
                .ToListAsync();
        }

        public async Task<StockProcessingResult> FindById(int id)
        {
            return await _db.StockProcessingResults
                .Include(x => x.StockProcessingSession)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<StockProcessingResult>> FindBySessionIdAsync(int sessionId)
        {
            return await _db.StockProcessingResults
                .Where(x => x.StockProcessingSessionId == sessionId && !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingResults.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingResult model)
        {
            _db.StockProcessingResults.Update(model);
            return await Save();
        }
    }
}
