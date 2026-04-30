using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositRequestAgentRepository(VMSServiceDbContext _db) : IDepositRequestAgentRepository
    {
        public async Task<ICollection<DepositRequestAgent>> FindAll()
            => await _db.DepositRequestAgents.ToListAsync();

        public async Task<DepositRequestAgent> FindById(Guid id)
            => await _db.DepositRequestAgents.FindAsync(id)
               ?? throw new KeyNotFoundException($"DepositRequestAgent {id} not found.");

        public async Task<bool> Create(DepositRequestAgent entity)
        {
            await _db.DepositRequestAgents.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(DepositRequestAgent entity)
        {
            _db.DepositRequestAgents.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> IsExists(Guid id)
            => await _db.DepositRequestAgents.AnyAsync(c => c.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<DepositRequestAgent>> GetByRequestAsync(
            Guid requestId, CancellationToken ct = default)
            => await _db.DepositRequestAgents
                .Where(c => c.DepositRequestId == requestId)
                .ToListAsync(ct);

        public async Task<IEnumerable<DepositRequestAgent>> GetByRequestAndSideAsync(
            Guid requestId, string side, CancellationToken ct = default)
            => await _db.DepositRequestAgents
                .Where(c => c.DepositRequestId == requestId && c.EnteredBy == side)
                .ToListAsync(ct);

        public void DeleteByRequestAndSide(Guid requestId, string side)
            => _db.DepositRequestAgents.RemoveRange(
                _db.DepositRequestAgents
                    .Where(c => c.DepositRequestId == requestId && c.EnteredBy == side));
    }
}