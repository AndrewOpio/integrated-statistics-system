using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositRequestRepository(VMSServiceDbContext _db) : IDepositRequestRepository
    {
        public async Task<ICollection<DepositRequest>> FindAll()
            => await _db.DepositRequests.ToListAsync();

        public async Task<DepositRequest> FindById(Guid id)
            => await _db.DepositRequests.FindAsync(id)
               ?? throw new KeyNotFoundException($"DepositRequest {id} not found.");

        public async Task<bool> Create(DepositRequest entity)
        {
            await _db.DepositRequests.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(DepositRequest entity)
        {
            _db.DepositRequests.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<DepositRequest?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
            => await _db.DepositRequests
                .Include(r => r.DepositRequestDenominations)
                .Include(r => r.DepositRequestAgents)
                .Include(r => r.DepositRequestStatusHistory)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task<int> CountByCbBranchAndDateAsync(int cbId, int bouBranchId, DateOnly date, CancellationToken ct = default)
            => await _db.DepositRequests
                .CountAsync(r => r.CbId == cbId
                              && r.BouBranchId == bouBranchId
                              && r.DepositDate == date, ct);

        public async Task<IEnumerable<DepositRequest>> GetByCbAsync(int cbId, CancellationToken ct = default)
            => await _db.DepositRequests
                .Where(r => r.CbId == cbId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync(ct);

        public async Task<bool> IsExists(Guid id)
            => await _db.DepositRequests.AnyAsync(d => d.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);
    }
}