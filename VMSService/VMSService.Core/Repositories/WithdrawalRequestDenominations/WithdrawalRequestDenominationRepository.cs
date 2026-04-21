using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using static VMSService.Core.Static.WithdrawStatuses;

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

        public async Task CopyToBouAsync(Guid requestId, string userId, CancellationToken ct)
        {
            var cbLines = await _db.WithdrawalRequestDenominations
                .Where(d => d.WithdrawalRequestId == requestId
                         && d.Origin == WithdrawalOrigin.Cb)
                .ToListAsync(ct);

            foreach (var line in cbLines)
            {
                await _db.WithdrawalRequestDenominations.AddAsync(new WithdrawalRequestDenomination
                {
                    WithdrawalRequestId = requestId,
                    DenominationId = line.DenominationId,
                    DenominationTypeId = line.DenominationTypeId,
                    Bundles = line.Bundles,
                    Boxes = line.Boxes,
                    Amount = line.Amount,
                    Origin = WithdrawalOrigin.Bou,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now,
                }, ct);
            }
        }

        public async Task<bool> HasBouOriginAsync(Guid requestId, CancellationToken ct = default)
            => await _db.WithdrawalRequestDenominations
            .AnyAsync(d => d.WithdrawalRequestId == requestId && d.Origin == WithdrawalOrigin.Bou, ct);

        public async Task DeleteBouRowsWithChildrenAsync(Guid requestId, CancellationToken ct = default)
        {
            var bankRows = await _db.WithdrawalRequestDenominations
                .Include(d => d.DispatchedBoxes)
                .Where(d => d.WithdrawalRequestId == requestId && d.Origin == WithdrawalOrigin.Bou)
                .ToListAsync(ct);

            if (bankRows.Count == 0) return;

            var boxes = bankRows.SelectMany(r => r.DispatchedBoxes).ToList();
            if (boxes.Count > 0)
                _db.WithdrawalRequestDenominationBoxes.RemoveRange(boxes);

            _db.WithdrawalRequestDenominations.RemoveRange(bankRows);
        }

    }
}
