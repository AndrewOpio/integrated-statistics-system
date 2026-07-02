using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionAuthorizationStatusHistories
{
    public class DestructionAuthorizationStatusHistoryRepository(VMSServiceDbContext _db) : IDestructionAuthorizationStatusHistoryRepository
    {
        public async Task<bool> Create(DestructionAuthorizationStatusHistory model)
        {
            _db.DestructionAuthorizationStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<DestructionAuthorizationStatusHistory>> FindAll()
        {
            return await _db.DestructionAuthorizationStatusHistory
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<DestructionAuthorizationStatusHistory> FindById(int id)
        {
            return await _db.DestructionAuthorizationStatusHistory
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<DestructionAuthorizationStatusHistory>> FindByDestructionAuthorizationId(int authorizationId)
        {
            return await _db.DestructionAuthorizationStatusHistory
                .Where(x => x.DestructionAuthorizationId == authorizationId && !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionAuthorizationStatusHistory.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Update(DestructionAuthorizationStatusHistory model)
        {
            _db.DestructionAuthorizationStatusHistory.Update(model);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
