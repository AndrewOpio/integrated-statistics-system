using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CountingRoomSessions
{
    public class CountingRoomSessionRepository(VMSServiceDbContext _db) : ICountingRoomSessionRepository
    {
        public async Task<bool> Create(CountingRoomSession model)
        {
            _db.CountingRoomSessions.Add(model);
            return await Save();
        }

        public async Task<ICollection<CountingRoomSession>> FindAll()
        {
            return await _db.CountingRoomSessions
                .Where(x => !x.IsDeleted)
                .Include(x => x.CountingRoom)
                .Include(x => x.StockProcessingPlan)
                .ToListAsync();
        }

        public async Task<CountingRoomSession> FindById(int id)
        {
            return await _db.CountingRoomSessions
                .Include(x => x.CountingRoom)
                .Include(x => x.StockProcessingPlan)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CountingRoomSessions
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CountingRoomSession model)
        {
            _db.CountingRoomSessions.Update(model);
            return await Save();
        }

        public async Task<ICollection<CountingRoomSession>> FindByStockProcessingPlanId(int stockProcessingPlanId)
        {
            return await _db.CountingRoomSessions
                .Where(x => x.StockProcessingPlanId == stockProcessingPlanId && !x.IsDeleted)
                .Include(x => x.CountingRoom)
                .Include(x => x.StockProcessingPlan)
                .ToListAsync();
        }
    }
}
