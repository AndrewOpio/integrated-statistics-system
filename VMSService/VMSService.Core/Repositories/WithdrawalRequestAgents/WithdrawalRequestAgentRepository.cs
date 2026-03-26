using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestAgents
{
    public class WithdrawalRequestAgentRepository(VMSServiceDbContext _db)
    : IWithdrawalRequestAgentRepository
    {
        public async Task<ICollection<WithdrawalRequestAgent>> FindAll()
            => await _db.WithdrawalRequestAgents.ToListAsync();

        public async Task<WithdrawalRequestAgent> FindById(Guid id)
            => await _db.WithdrawalRequestAgents.FindAsync(id)
               ?? throw new KeyNotFoundException($"WithdrawalRequestCollector {id} not found.");

        public async Task<bool> Create(WithdrawalRequestAgent entity)
        {
            await _db.WithdrawalRequestAgents.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(WithdrawalRequestAgent entity)
        {
            _db.WithdrawalRequestAgents.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> IsExists(Guid id)
            => await _db.WithdrawalRequestAgents.AnyAsync(c => c.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<WithdrawalRequestAgent>> GetByRequestAsync(
            Guid requestId, CancellationToken ct = default)
            => await _db.WithdrawalRequestAgents
                .Where(c => c.WithdrawalRequestId == requestId)
                .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequestAgent>> GetByRequestAndSideAsync(
            Guid requestId, string side, CancellationToken ct = default)
            => await _db.WithdrawalRequestAgents
                .Where(c => c.WithdrawalRequestId == requestId && c.EnteredBy == side)
                .ToListAsync(ct);

        public void DeleteByRequestAndSide(Guid requestId, string side)
            => _db.WithdrawalRequestAgents.RemoveRange(
                _db.WithdrawalRequestAgents
                  .Where(c => c.WithdrawalRequestId == requestId && c.EnteredBy == side));
    }
}
