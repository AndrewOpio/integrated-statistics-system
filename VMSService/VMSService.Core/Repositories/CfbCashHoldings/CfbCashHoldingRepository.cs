using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbCashHoldings
{
    public class CfbCashHoldingRepository(
        VMSServiceDbContext _db,
        IStockService _stockService,
        ILogger<CfbCashHoldingRepository> _logger) : ICfbCashHoldingRepository
    {
        // ── Reads — DB only, no external calls ──────────────────────────────

        public async Task<ICollection<CfbCashHolding>> FindAll()
            => await _db.CfbCashHoldings
                .Include(h => h.CfbBox)
                .ToListAsync();

        public async Task<ICollection<CfbCashHolding>> FindByCfbBoxId(int cfbBoxId, bool activeOnly = true)
        {
            var query = _db.CfbCashHoldings.Where(h => h.CfbBoxId == cfbBoxId);
            if (activeOnly)
                query = query.Where(h => h.IsActive);
            return await query.ToListAsync();
        }

        public async Task<CfbCashHolding?> FindById(int id)
            => await _db.CfbCashHoldings
                .Include(h => h.CfbBox)
                .FirstOrDefaultAsync(h => h.Id == id);

        public async Task<bool> IsExists(int id)
            => await _db.CfbCashHoldings.AnyAsync(h => h.Id == id);

        // ── Write ────────────────────────────────────────────────────────────

        public async Task SetBaseline(int cfbBoxId, int denominationId, long amount)
        {
            // IStockService.GetAllDenominations() swallows internal errors and returns empty on failure.
            var denoms = await _stockService.GetAllDenominations();
            if (!denoms.Any())
            {
                _logger.LogError(
                    "StockService returned no denominations while setting baseline for denom {DenomId} in box {BoxId}",
                    denominationId, cfbBoxId);
                throw new InvalidOperationException(
                    "Denomination service returned no data — cannot validate denomination before setting baseline.");
            }

            var denom = denoms.FirstOrDefault(d => d.Id == denominationId);
            if (denom is null)
                throw new InvalidOperationException(
                    $"Denomination {denominationId} was not found in StockService.");
            if (denom.IsDeleted)
                throw new InvalidOperationException(
                    $"Denomination {denominationId} has been deleted and cannot be used for a baseline.");

            // Cross-type guard: coin denom cannot go in a Notes box and vice-versa.
            var denomTypeName = denom.DenominationType?.Name;
            if (!string.IsNullOrEmpty(denomTypeName))
            {
                var box = await _db.CfbBoxes.FindAsync(cfbBoxId);
                if (box is not null && !string.Equals(denomTypeName, box.BoxType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Denomination {denominationId} is a {denomTypeName} denomination " +
                        $"but CfbBox {cfbBoxId} is a {box.BoxType} box. Types must match.");
            }

            // Deactivate any current active baseline for this slot.
            // TODO (top-up path): before this point, the caller must have confirmed that the
            //      approval workflow service has authorised the baseline change.
            var existing = await _db.CfbCashHoldings
                .FirstOrDefaultAsync(h => h.CfbBoxId == cfbBoxId
                                       && h.DenominationId == denominationId
                                       && h.IsActive);
            if (existing is not null)
                existing.IsActive = false;

            // Insert new active baseline row — snapshot DenomValue so math never drifts.
            await _db.CfbCashHoldings.AddAsync(new CfbCashHolding
            {
                CfbBoxId = cfbBoxId,
                DenominationId = denominationId,
                DenomValue = (int)denom.Value,
                Amount = amount,
                IsActive = true,
            });
        }

        public async Task SetOpeningBalance(int cfbBoxId, IReadOnlyDictionary<int, long> amountsByDenomId)
        {
            var denoms = await _stockService.GetAllDenominations();
            if (!denoms.Any())
            {
                _logger.LogError(
                    "StockService returned no denominations while setting opening balance for box {BoxId}", cfbBoxId);
                throw new InvalidOperationException(
                    "Denomination service returned no data — cannot set opening balance.");
            }

            var denomById = denoms.ToDictionary(d => d.Id);
            var box = await _db.CfbBoxes.FindAsync(cfbBoxId)
                      ?? throw new InvalidOperationException($"CfbBox {cfbBoxId} not found.");

            foreach (var (denomId, amount) in amountsByDenomId)
            {
                if (amount <= 0)
                    continue; // blank/zero = skip, denom stays in "needs baseline" pool

                if (await _db.CfbCashHoldings.AnyAsync(h => h.CfbBoxId == cfbBoxId
                                                          && h.DenominationId == denomId
                                                          && h.IsActive))
                    throw new InvalidOperationException(
                        $"Denomination {denomId} already has an active baseline in box {cfbBoxId}. " +
                        "Use a top-up to change an existing baseline.");

                if (!denomById.TryGetValue(denomId, out var denom))
                    throw new InvalidOperationException($"Denomination {denomId} not found in StockService.");
                if (denom.IsDeleted)
                    throw new InvalidOperationException($"Denomination {denomId} is deleted.");
                if (!string.IsNullOrEmpty(denom.DenominationType?.Name) &&
                    !string.Equals(denom.DenominationType.Name, box.BoxType, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Denomination {denomId} ({denom.DenominationType.Name}) does not match " +
                        $"box type {box.BoxType}.");

                await _db.CfbCashHoldings.AddAsync(new CfbCashHolding
                {
                    CfbBoxId = cfbBoxId,
                    DenominationId = denomId,
                    DenomValue = (int)denom.Value,
                    Amount = amount,
                    IsActive = true,
                });
            }
        }

        public async Task<CfbCashHolding?> FindActiveByBoxAndDenom(int cfbBoxId, int denominationId)
            => await _db.CfbCashHoldings
                .FirstOrDefaultAsync(h => h.CfbBoxId == cfbBoxId
                                       && h.DenominationId == denominationId
                                       && h.IsActive);

        public async Task Create(CfbCashHolding holding)
            => await _db.CfbCashHoldings.AddAsync(holding);

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}