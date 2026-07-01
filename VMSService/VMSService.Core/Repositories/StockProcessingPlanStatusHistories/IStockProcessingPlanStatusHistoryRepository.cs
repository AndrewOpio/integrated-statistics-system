using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingPlanStatusHistories
{
    public interface IStockProcessingPlanStatusHistoryRepository : IRepositoryBase<StockProcessingPlanStatusHistory, int>
    {
        Task<ICollection<StockProcessingPlanStatusHistory>> FindByStockProcessingPlanId(int stockProcessingPlanId);
    }
}
