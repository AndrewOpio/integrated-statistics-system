using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlans
{
    public interface IStockProcessingPlanRepository : IRepositoryBase<StockProcessingPlan, int>
    {
        Task<PlanBagSummaryResult> GetBagSummaryAsync(int planId);

        /// <summary>
        /// Per-denomination Reissue/Cancelled/Shredded totals for the plan's sessions, taken from
        /// each session's single latest (most recent) non-deleted result row — result rows are
        /// cumulative snapshots, so only the newest row per session reflects that session's true
        /// totals. Prefers verified rows when a session has any (ConfirmResults runs after
        /// VerifyResults). Grouped by each session's own DenominationId.
        /// </summary>
        Task<PlanConfirmedResultsSummary> GetConfirmedResultsSummaryAsync(int planId);
    }
}
