using Microsoft.EntityFrameworkCore;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositRequestDenominationRepository(VMSServiceDbContext _db) : IDepositRequestDenominationRepository
    {
        public async Task<ICollection<DepositRequestDenomination>> FindAll()
            => await _db.DepositRequestDenominations.ToListAsync();

        public async Task<DepositRequestDenomination> FindById(Guid id)
            => await _db.DepositRequestDenominations.FindAsync(id)
               ?? throw new KeyNotFoundException($"DepositRequestDenomination {id} not found.");

        public async Task<bool> Create(DepositRequestDenomination entity)
        {
            await _db.DepositRequestDenominations.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(DepositRequestDenomination entity)
        {
            _db.DepositRequestDenominations.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> IsExists(Guid id)
            => await _db.DepositRequestDenominations.AnyAsync(d => d.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        public async Task<IEnumerable<DepositRequestDenomination>> GetByRequestAsync(
            Guid requestId, CancellationToken ct = default)
            => await _db.DepositRequestDenominations
                .Where(d => d.DepositRequestId == requestId)
                .ToListAsync(ct);

        public async Task<IEnumerable<DepositRequestDenomination>> GetByRequestAndOriginAsync(
            Guid requestId, string origin, CancellationToken ct = default)
            => await _db.DepositRequestDenominations
                .Where(d => d.DepositRequestId == requestId && d.Origin == origin)
                .ToListAsync(ct);

        public void DeleteByRequest(Guid requestId, string origin)
            => _db.DepositRequestDenominations.RemoveRange(
                _db.DepositRequestDenominations
                    .Where(d => d.DepositRequestId == requestId && d.Origin == origin));

        public async Task CopyToBouAsync(Guid requestId, string userId, CancellationToken ct)
        {
            var cbLines = await _db.DepositRequestDenominations
                .Where(d => d.DepositRequestId == requestId
                          && d.Origin == DepositStatuses.DepositOrigin.Cb)
                .ToListAsync(ct);

            foreach (var line in cbLines)
            {
                await _db.DepositRequestDenominations.AddAsync(new DepositRequestDenomination
                {
                    DepositRequestId = requestId,
                    DenominationId = line.DenominationId,
                    DenominationValue = line.DenominationValue,
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    SubCategoryId = line.SubCategoryId,
                    Origin = DepositStatuses.DepositOrigin.Bou,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                }, ct);
            }
        }
    }
}