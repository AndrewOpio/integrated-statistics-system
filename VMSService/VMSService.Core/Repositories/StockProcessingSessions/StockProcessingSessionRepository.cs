using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingSessions
{
    public class StockProcessingSessionRepository(VMSServiceDbContext _db) : IStockProcessingSessionRepository
    {
        public async Task<bool> Create(StockProcessingSession model)
        {
            _db.StockProcessingSessions.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingSession>> FindAll()
        {
            return await _db.StockProcessingSessions
                .Where(s => !s.IsDeleted)
                .Include(s => s.ProcessingBoard)
                .Include(s => s.ProcessingSessionBags)
                    .ThenInclude(psb => psb.Bag)
                .ToListAsync();
        }

        public async Task<StockProcessingSession> FindById(int id)
        {
            return await _db.StockProcessingSessions
                .Include(s => s.ProcessingBoard)
                .Include(s => s.ProcessingSessionBags)
                    .ThenInclude(psb => psb.Bag)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<StockProcessingSession>> GetByPlanIdAsync(int planId)
        {
            return await _db.StockProcessingSessions
                .Include(s => s.ProcessingBoard)
                    .ThenInclude(b => b!.BPSMachine)
                .Include(s => s.ProcessingSessionBags.Where(psb => !psb.IsDeleted))
                    .ThenInclude(psb => psb.Bag)
                .Where(s => s.StockProcessingPlanId == planId && !s.IsDeleted)
                .OrderBy(s => s.ProcessingBoardId)
                .ThenBy(s => s.Id)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingSessions.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingSession model)
        {
            _db.StockProcessingSessions.Update(model);
            return await Save();
        }
    }
}
