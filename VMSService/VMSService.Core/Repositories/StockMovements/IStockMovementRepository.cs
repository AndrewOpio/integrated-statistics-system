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

        /// <summary>
        /// Explicitly stages deletion of the given denominations and their box-number-range/bag children.
        /// Required because a global EF configuration rule forces all non-owned foreign keys to
        /// DeleteBehavior.Restrict, which disables EF's automatic deletion of orphaned dependents —
        /// simply clearing a StockMovement's Denominations collection throws
        /// "the association ... has been severed" instead of deleting the rows. Does not call SaveChanges;
        /// the caller's own SaveAsync()/SaveChangesAsync() persists this alongside any other changes.
        /// </summary>
        Task RemoveDenominationsAsync(IEnumerable<StockMovementDenomination> denominations);
    }
}
