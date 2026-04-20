using System.Collections.Generic;
using System.Threading.Tasks;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockMovements
{
    public interface IStockMovementRepository
    {
        Task<StockMovement> CreateAsync(StockMovement movement);
        Task<StockMovement> FindByIdAsync(int id);
        Task<StockMovement> FindByIdForEditAsync(int id);
        Task<List<StockMovement>> FindByIdsAsync(List<int> branches);
        Task<ICollection<StockMovement>> FindAllAsync();
        Task<bool> UpdateAsync(StockMovement movement);
        Task<bool> DeleteAsync(int id);
        Task<bool> SaveAsync();
        Task<bool> IsExistsAsync(int id);
        Task<bool> SaveReceivedDenominationsAsync(int stockMovementId, List<StockMovementDenomination> receivedDenominations);
    }
}
