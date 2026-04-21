using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AgentDetailStatusHistories
{
    public class AgentDetailStatusHistoryRepository(VMSServiceDbContext _db) : IAgentDetailStatusHistoryRepository
    {
        public async Task<bool> Create(AgentDetailStatusHistory model)
        {
            _db.AgentDetailStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<AgentDetailStatusHistory>> FindAll()
        {
            return await _db.AgentDetailStatusHistory.Where(x => x.IsDeleted == false)
                .Include(x => x.AgentDetail)
                .ToListAsync();
        }

        public async Task<AgentDetailStatusHistory> FindById(int id)
        {
            return await _db.AgentDetailStatusHistory
                .Include(x => x.AgentDetail)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.AgentDetailStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(AgentDetailStatusHistory model)
        {
            _db.AgentDetailStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<AgentDetailStatusHistory>> FindByAgentDetailId(int agentDetailId)
        {
            return await _db.AgentDetailStatusHistory
                .Where(x => x.AgentDetailId == agentDetailId && x.IsDeleted == false)
                .Include(x => x.AgentDetail)
                .ToListAsync();
        }
    }
}