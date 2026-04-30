using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositRequestStatusHistoryRepository(VMSServiceDbContext _db) : IDepositRequestStatusHistoryRepository
    {
        public async Task<ICollection<DepositRequestStatusHistory>> FindAll()
            => await _db.DepositRequestStatusHistory.ToListAsync();

        public async Task<DepositRequestStatusHistory> FindById(Guid id)
            => await _db.DepositRequestStatusHistory.FindAsync(id)
               ?? throw new KeyNotFoundException($"DepositRequestStatusHistory {id} not found.");

        public async Task<bool> Create(DepositRequestStatusHistory entity)
        {
            await _db.DepositRequestStatusHistory.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(DepositRequestStatusHistory entity)
        {
            _db.DepositRequestStatusHistory.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> IsExists(Guid id)
            => await _db.DepositRequestStatusHistory.AnyAsync(s => s.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<DepositRequestStatusHistory>> GetByRequestAsync(
            Guid requestId, CancellationToken ct = default)
            => await _db.DepositRequestStatusHistory
                .Where(s => s.DepositRequestId == requestId)
                .OrderBy(s => s.ChangedAt)
                .ToListAsync(ct);
    }
}