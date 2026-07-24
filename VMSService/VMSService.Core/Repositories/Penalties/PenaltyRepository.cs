using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Penalties
{
    public class PenaltyRepository(VMSServiceDbContext _db, ILogger<PenaltyRepository> _logger) : IPenaltyRepository
    {
        public async Task<ICollection<Penalty>> FindAll()
            => await _db.Penalties.OrderBy(p => p.Name).ToListAsync();

        public async Task<ICollection<Penalty>> FindByActive(bool active)
            => await _db.Penalties.Where(p => p.Active == active).OrderBy(p => p.Name).ToListAsync();

        public async Task<ICollection<Penalty>> FindApplicableFor(string appliesTo)
            => await _db.Penalties
                .Where(p => p.Active && p.AppliesTo == appliesTo)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<Penalty> FindById(int id)
            => await _db.Penalties.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<bool> IsExists(int id)
            => await _db.Penalties.AnyAsync(p => p.Id == id);

        public async Task<bool> Create(Penalty entity)
        {
            ValidateRateOption(entity);
            await _db.Penalties.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(Penalty entity)
        {
            ValidateRateOption(entity);
            _db.Penalties.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> Save() => await _db.SaveChangesAsync() > 0;

        // Pure — no DB access. Infers the formula from whichever rate field is set (ValidateRateOption
        // guarantees exactly one is, for penalties saved through this repository).
        public decimal Compute(Penalty penalty, decimal quantity = 1, decimal denomValue = 0,
                                decimal baseValue = 0, decimal correctlyClassifiedValue = 0)
        {
            if (penalty.PenaltyValue.HasValue)
                return penalty.PenaltyValue.Value * quantity;

            if (penalty.PenaltyMultiplier.HasValue)
                return penalty.PenaltyMultiplier.Value * denomValue * quantity;

            if (penalty.PenaltyPercent.HasValue)
            {
                if (penalty.PercentCutOff.HasValue && baseValue != 0
                    && correctlyClassifiedValue / baseValue >= penalty.PercentCutOff.Value)
                    return 0m;

                return penalty.PenaltyPercent.Value * baseValue;
            }

            _logger.LogError(
                "Penalty {PenaltyId} ({PenaltyName}) has no rate field set — cannot compute a charge.",
                penalty.Id, penalty.Name);
            throw new InvalidOperationException(
                $"'{penalty.Name}' isn't configured with a charge rule — please contact an administrator.");
        }

        // Exactly one rate option must be set: PenaltyValue, PenaltyPercent (with PercentCutOff), or PenaltyMultiplier.
        private static void ValidateRateOption(Penalty entity)
        {
            var optionsSet = new[] { entity.PenaltyValue.HasValue, entity.PenaltyPercent.HasValue, entity.PenaltyMultiplier.HasValue }
                .Count(isSet => isSet);

            if (optionsSet != 1)
                throw new InvalidOperationException(
                    "Exactly one rate option must be set: Penalty Value, Penalty Percent, or Penalty Multiplier.");

            if (entity.PenaltyPercent.HasValue && !entity.PercentCutOff.HasValue)
                throw new InvalidOperationException("Penalty Percent requires a Percent Cut-Off value.");
        }
    }
}
