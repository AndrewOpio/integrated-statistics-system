using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakeSystemPositions
{
    public interface IStockTakeSystemPositionRepository
    {
        Task<bool> CreateRange(ICollection<StockTakeSystemPosition> models);
        Task<ICollection<StockTakeSystemPosition>> FindByStockTakeId(int stockTakeId);

        // Clears out the previous snapshot before an edit re-captures it fresh.
        Task<bool> DeleteByStockTakeId(int stockTakeId);
    }
}
