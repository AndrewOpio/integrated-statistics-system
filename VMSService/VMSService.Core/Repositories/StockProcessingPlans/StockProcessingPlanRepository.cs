using Microsoft.EntityFrameworkCore;
using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlans
{
    public class StockProcessingPlanRepository(VMSServiceDbContext _db) : IStockProcessingPlanRepository
    {
        public async Task<bool> Create(StockProcessingPlan model)
        {
            _db.StockProcessingPlans.Add(model);
            return await Save();
        }

        public async Task<ICollection<StockProcessingPlan>> FindAll()
        {
            return await _db.StockProcessingPlans
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<StockProcessingPlan> FindById(int id)
        {
            return await _db.StockProcessingPlans
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StockProcessingPlans
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StockProcessingPlan model)
        {
            _db.StockProcessingPlans.Update(model);
            return await Save();
        }

        public async Task<PlanBagSummaryResult> GetBagSummaryAsync(int planId)
        {
            var bagIds = await _db.SessionBags
                .Where(sb => sb.StockProcessingPlanId == planId && !sb.IsDeleted)
                .Select(sb => sb.BagId)
                .Distinct()
                .ToListAsync();

            if (bagIds.Count == 0)
                return new PlanBagSummaryResult();

            var bagDenomData = await _db.BagDenominations
                .Where(bd => bagIds.Contains(bd.BagId) && !bd.IsDeleted)
                .Select(bd => new
                {
                    bd.BagId,
                    bd.DepositRequestDenomination.DenominationId,
                    bd.DepositRequestDenomination.DenominationTypeId,
                    bd.DepositRequestDenomination.DenominationValue,
                    bd.NumberOfBundlesOrBoxes
                })
                .ToListAsync();

            var grandTotalValue = bagDenomData.Sum(x => x.DenominationValue * 1000 * x.NumberOfBundlesOrBoxes);

            var denominations = bagDenomData
                .GroupBy(x => x.DenominationId)
                .Select(g =>
                {
                    //denominationTypeNamesById.TryGetValue(g.First().DenominationTypeId, out var typeName);
                    //var containerType = typeName == StockConstants.DenominationTypes.Coin
                    //    ? StockConstants.ContainerTypes.Box
                    //    : StockConstants.ContainerTypes.Bag;

                    return new StockMovementDenomination
                    {
                        DenominationId = g.Key,
                        SubTotalValue = g.Sum(x => x.DenominationValue * 1000 * x.NumberOfBundlesOrBoxes),
                        NumBundles = g.Sum(x => x.NumberOfBundlesOrBoxes),
                        NumBagsBoxes = g.Select(x => x.BagId).Distinct().Count(),
                        //ContainerType = containerType
                    };
                })
                .ToList();

            return new PlanBagSummaryResult
            {
                TotalNumberOfBagsOrBoxes = bagIds.Count,
                GrandTotalValue = grandTotalValue,
                Denominations = denominations
            };
        }

        public async Task<PlanConfirmedResultsSummary> GetConfirmedResultsSummaryAsync(int planId)
        {
            var sessionData = await _db.StockProcessingSessions
                .Where(s => s.StockProcessingPlanId == planId && !s.IsDeleted)
                .Select(s => new { s.Id, s.DenominationId })
                .ToListAsync();

            if (sessionData.Count == 0)
                return new PlanConfirmedResultsSummary();

            var sessionIds = sessionData.Select(s => s.Id).ToList();
            var denomBySessionId = sessionData.ToDictionary(s => s.Id, s => s.DenominationId);

            var allResults = await _db.StockProcessingResults
                .Where(r => sessionIds.Contains(r.StockProcessingSessionId) && !r.IsDeleted)
                .Select(r => new
                {
                    r.StockProcessingSessionId,
                    r.CreatedDate,
                    r.Id,
                    r.IsVerified,
                    r.ReissueValue,
                    r.CanceledValue,
                    r.FitShredValue
                })
                .ToListAsync();

            if (allResults.Count == 0)
                return new PlanConfirmedResultsSummary();

            // Each result row is a cumulative snapshot of its session, not a delta — take only the
            // latest (most recent) row per session, preferring verified rows when any exist.
            var latestPerSession = allResults
                .GroupBy(r => r.StockProcessingSessionId)
                .Select(g =>
                {
                    var verified = g.Where(r => r.IsVerified).ToList();
                    var pool = verified.Count > 0 ? verified : g.ToList();
                    return pool.OrderByDescending(r => r.CreatedDate).ThenByDescending(r => r.Id).First();
                })
                .ToList();

            var denominations = latestPerSession
                .GroupBy(r => denomBySessionId.GetValueOrDefault(r.StockProcessingSessionId))
                .Select(g => new SessionResultDenominationSummary
                {
                    DenominationId = g.Key,
                    ReissueValue = g.Sum(r => r.ReissueValue),
                    CanceledValue = g.Sum(r => r.CanceledValue),
                    FitShredValue = g.Sum(r => r.FitShredValue)
                })
                .ToList();

            return new PlanConfirmedResultsSummary
            {
                Denominations = denominations,
                TotalReissueValue = denominations.Sum(d => d.ReissueValue),
                TotalCanceledValue = denominations.Sum(d => d.CanceledValue),
                TotalFitShredValue = denominations.Sum(d => d.FitShredValue)
            };
        }
    }
}
