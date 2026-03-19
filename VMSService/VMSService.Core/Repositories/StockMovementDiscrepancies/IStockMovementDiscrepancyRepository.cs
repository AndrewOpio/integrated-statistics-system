using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockMovementDiscrepancies
{
    public interface IStockMovementDiscrepancyRepository
    {
        Task<List<StockMovementDiscrepancy>> GetByStockMovementIdAsync(int stockMovementId);
        Task<StockMovementDiscrepancy> GetByIdAsync(int id);
        Task AddAsync(StockMovementDiscrepancy discrepancy);
        Task UpdateAsync(StockMovementDiscrepancy discrepancy);
    }
}
