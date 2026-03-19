using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockMovementStatusHistories
{
    public interface IStockMovementStatusHistoryRepository : IRepositoryBase<StockMovementStatusHistory, int>
    {
        Task<ICollection<StockMovementStatusHistory>> FindByStockMovementId(int stockMovementId);
    }
}
