using VMSService.Core.Models.StockProcessingPlans;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlans
{
    public interface IStockProcessingPlanRepository : IRepositoryBase<StockProcessingPlan, int>
    {
        Task<PlanBagSummaryResult> GetBagSummaryAsync(int planId);
        Task<PlanBagSummaryResult> GetReIssueResultSummaryAsync(int planId);
        Task<PlanBagSummaryResult> GetCanceledResultSummaryAsync(int planId);
    }
}
