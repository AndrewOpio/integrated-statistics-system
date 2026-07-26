using Microsoft.EntityFrameworkCore;
using VMSService.Core.Repositories.Penalties;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AppliedPenalties
{
    public class AppliedPenaltyRepository(VMSServiceDbContext _db, IPenaltyRepository _penaltyRepository) : IAppliedPenaltyRepository
    {
        public async Task<ICollection<AppliedPenalty>> FindAll()
            => await _db.AppliedPenalties.Include(a => a.Penalty).ToListAsync();

        public async Task<ICollection<AppliedPenalty>> FindBySourceId(string sourceId)
            => await _db.AppliedPenalties
                .Include(a => a.Penalty)
                .Where(a => a.SourceId == sourceId)
                .OrderBy(a => a.Penalty!.Name)
                .ToListAsync();

        public async Task<AppliedPenalty> FindById(int id)
            => await _db.AppliedPenalties.Include(a => a.Penalty).FirstOrDefaultAsync(a => a.Id == id);

        public async Task<bool> IsExists(int id)
            => await _db.AppliedPenalties.AnyAsync(a => a.Id == id);

        public async Task<bool> Create(AppliedPenalty entity)
        {
            await _db.AppliedPenalties.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(AppliedPenalty entity)
        {
            _db.AppliedPenalties.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> Save() => await _db.SaveChangesAsync() > 0;

        public async Task<AppliedPenalty> Apply(string source, string sourceId, int penaltyId, int? quantity = null,
                                                  decimal? denomValue = null, decimal? baseValue = null,
                                                  decimal? correctlyClassifiedValue = null)
        {
            var penalty = await _penaltyRepository.FindById(penaltyId)
                ?? throw new InvalidOperationException($"Penalty {penaltyId} was not found.");

            var chargeValue = _penaltyRepository.Compute(
                penalty,
                quantity: quantity ?? 1,
                denomValue: denomValue ?? 0,
                baseValue: baseValue ?? 0,
                correctlyClassifiedValue: correctlyClassifiedValue ?? 0);

            var existing = await _db.AppliedPenalties
                .FirstOrDefaultAsync(a => a.SourceId == sourceId && a.PenaltyId == penaltyId);

            if (existing is not null)
            {
                existing.ChargeValue = chargeValue;
                existing.DenomValue = denomValue;
                existing.Quantity = quantity;
                return existing;
            }

            var applied = new AppliedPenalty
            {
                Source = source,
                SourceId = sourceId,
                PenaltyId = penaltyId,
                ChargeValue = chargeValue,
                DenomValue = denomValue,
                Quantity = quantity,
            };
            await _db.AppliedPenalties.AddAsync(applied);
            return applied;
        }

        public async Task Remove(int appliedPenaltyId)
        {
            var existing = await _db.AppliedPenalties.FirstOrDefaultAsync(a => a.Id == appliedPenaltyId);
            if (existing is not null)
                _db.AppliedPenalties.Remove(existing);
        }
    }
}
