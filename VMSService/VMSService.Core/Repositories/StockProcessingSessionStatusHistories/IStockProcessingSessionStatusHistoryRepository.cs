using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingSessionStatusHistories
{
    public interface IStockProcessingSessionStatusHistoryRepository : IRepositoryBase<StockProcessingSessionStatusHistory, int>
    {
        Task<ICollection<StockProcessingSessionStatusHistory>> FindByStockProcessingSessionId(int stockProcessingSessionId);
    }
}
