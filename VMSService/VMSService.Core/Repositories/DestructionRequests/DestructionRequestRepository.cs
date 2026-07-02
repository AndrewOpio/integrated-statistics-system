using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Core.Repository.Common;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionRequests
{
    public class DestructionRequestRepository(VMSServiceDbContext _db,
        ILogger<DestructionRequestRepository> _logger) : IDestructionRequestRepository
    {
        public async Task<bool> Create(DestructionRequest request)
        {
            await _db.DestructionRequests.AddAsync(request);
            return await Save();
        }

        public async Task<ICollection<DestructionRequest>> FindAll()
        {
            return await _db.DestructionRequests
                .AsNoTracking()
                .Include(r => r.DestructionAuthorization)
                .Include(r => r.DestructionType)
                .Include(r => r.DestructionLocation)
                .Include(r => r.DestructionRequestDenominations)
                .Include(r => r.DestructionRequestMembers)
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        public async Task<DestructionRequest> FindById(int id)
        {
            var result = await _db.DestructionRequests
                .Include(r => r.DestructionAuthorization)
                .Include(r => r.DestructionType)
                .Include(r => r.DestructionLocation)
                .Include(r => r.DestructionRequestDenominations)
                .Include(r => r.DestructionRequestMembers)
                .FirstOrDefaultAsync(r => r.Id == id);
            return result!;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionRequests.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(DestructionRequest entity)
        {
            _db.DestructionRequests.Update(entity);
            return await Save();
        }

        public async Task<bool> UpdateWithChildren(DestructionRequest entity)
        {
            var oldDenoms = _db.DestructionRequestDenominations
                .Where(d => d.DestructionRequestId == entity.Id);
            _db.DestructionRequestDenominations.RemoveRange(oldDenoms);

            var oldMembers = _db.DestructionRequestMembers
                .Where(m => m.DestructionRequestId == entity.Id);
            _db.DestructionRequestMembers.RemoveRange(oldMembers);

            _db.DestructionRequests.Update(entity);
            return await Save();
        }

        public async Task<bool> UpdateDenominationDestroyed(int denominationId, decimal destroyed)
        {
            var denom = await _db.DestructionRequestDenominations.FindAsync(denominationId);
            if (denom == null) return false;
            denom.Destroyed = destroyed;
            return await Save();
        }

        public async Task<ICollection<DestructionType>> GetAllDestructionTypes()
        {
            return await _db.DestructionTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
