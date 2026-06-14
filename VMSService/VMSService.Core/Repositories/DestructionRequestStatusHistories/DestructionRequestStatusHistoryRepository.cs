using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionRequestStatusHistories
{
    public class DestructionRequestStatusHistoryRepository(VMSServiceDbContext _db) : IDestructionRequestStatusHistoryRepository
    {
        public async Task<bool> Create(DestructionRequestStatusHistory model)
        {
            _db.DestructionRequestStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<DestructionRequestStatusHistory>> FindAll()
        {
            return await _db.DestructionRequestStatusHistory
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<DestructionRequestStatusHistory> FindById(int id)
        {
            return await _db.DestructionRequestStatusHistory
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<DestructionRequestStatusHistory>> FindByDestructionRequestId(int destructionRequestId)
        {
            return await _db.DestructionRequestStatusHistory
                .Where(x => x.DestructionRequestId == destructionRequestId && !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionRequestStatusHistory.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(DestructionRequestStatusHistory model)
        {
            _db.DestructionRequestStatusHistory.Update(model);
            return await Save();
        }
    }
}
