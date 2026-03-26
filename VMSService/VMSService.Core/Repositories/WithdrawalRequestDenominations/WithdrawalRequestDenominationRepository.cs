using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestDenominations
{
    public class WithdrawalRequestDenominationRepository(VMSServiceDbContext _db) : IWithdrawalRequestDenominationRepository
    {
        public async Task<ICollection<WithdrawalRequestDenomination>> FindAll()
            => await _db.WithdrawalRequestDenominations.ToListAsync();

        public async Task<WithdrawalRequestDenomination> FindById(Guid id)
        => await _db.WithdrawalRequestDenominations.FindAsync(id)
           ?? throw new KeyNotFoundException($"Withdrawal Request Denomination {id} not found.");

        public async Task<bool> Create(WithdrawalRequestDenomination entity)
        {
            await _db.WithdrawalRequestDenominations.AddAsync(entity);
            return true;
        }
        public Task<bool> Update(WithdrawalRequestDenomination entity)
        {
            _db.WithdrawalRequestDenominations.Update(entity);
            return Task.FromResult(true);
        }
        public async Task<bool> IsExists(Guid id)
        => await _db.WithdrawalRequestDenominations.AnyAsync(d => d.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAsync(
        Guid requestId, CancellationToken ct = default)
        => await _db.WithdrawalRequestDenominations
            .Where(d => d.WithdrawalRequestId == requestId)
            .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAndOriginAsync(
        Guid requestId, string origin, CancellationToken ct = default)
        => await _db.WithdrawalRequestDenominations
            .Where(d => d.WithdrawalRequestId == requestId && d.Origin == origin)
            .ToListAsync(ct);

        public void DeleteByRequest(Guid requestId, string origin)
        => _db.WithdrawalRequestDenominations.RemoveRange(
            _db.WithdrawalRequestDenominations
              .Where(d => d.WithdrawalRequestId == requestId && d.Origin == origin));

    }
}
