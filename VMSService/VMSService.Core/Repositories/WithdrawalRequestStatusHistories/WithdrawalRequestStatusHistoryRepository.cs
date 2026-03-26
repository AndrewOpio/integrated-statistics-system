using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestStatusHistories
{
    public class WithdrawalRequestStatusHistoryRepository(VMSServiceDbContext _db) : IWithdrawalRequestStatusHistoryRepository
    {
        public async Task<ICollection<WithdrawalRequestStatusHistory>> FindAll()
        => await _db.WithdrawalRequestStatusHistory.ToListAsync();

        public async Task<WithdrawalRequestStatusHistory> FindById(Guid id)
            => await _db.WithdrawalRequestStatusHistory.FindAsync(id)
               ?? throw new KeyNotFoundException($"WithdrawalRequestStatus {id} not found.");

        public async Task<bool> Create(WithdrawalRequestStatusHistory entity)
        {
            await _db.WithdrawalRequestStatusHistory.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(WithdrawalRequestStatusHistory entity)
        {
            _db.WithdrawalRequestStatusHistory.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> IsExists(Guid id)
            => await _db.WithdrawalRequestStatusHistory.AnyAsync(s => s.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<WithdrawalRequestStatusHistory>> GetByRequestAsync(
            Guid requestId, CancellationToken ct = default)
            => await _db.WithdrawalRequestStatusHistory
                .Where(s => s.WithdrawalRequestId == requestId)
                .OrderBy(s => s.ChangedAt)
                .ToListAsync(ct);
    }
}
