using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AgentDetails
{
    public class AgentDetailRepository(VMSServiceDbContext _db) : IAgentDetailRepository
    {
        public async Task<bool> Create(AgentDetail model)
        {
            _db.AgentDetails.Add(model);
            return await Save();
        }

        public async Task<ICollection<AgentDetail>> FindAll()
        {
            return await _db.AgentDetails.Where(x => x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<AgentDetail> FindById(int id)
        {
            return await _db.AgentDetails
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.AgentDetails
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(AgentDetail model)
        {
            _db.AgentDetails.Update(model);
            return await Save();
        }
    }
}