using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakeStatusHistories
{
    public interface IStockTakeStatusHistoryRepository
    {
        Task<bool> Create(StockTakeStatusHistory model);
        Task<ICollection<StockTakeStatusHistory>> FindByStockTakeId(int stockTakeId);
    }
}
