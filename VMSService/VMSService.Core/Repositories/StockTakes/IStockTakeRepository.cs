using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockTakes
{
    public interface IStockTakeRepository
    {
        Task<bool> Create(StockTake model);
        Task<ICollection<StockTake>> FindAll();
        Task<StockTake> FindById(int id);
        Task<bool> IsExists(int id);
        Task<bool> Update(StockTake model);

        // The amendment record that was created against the given original StockTake, if any.
        Task<StockTake?> FindAmendmentOf(int stockTakeId);

        // Used to derive the next sequence number for a branch+date's reference number.
        // excludeId lets an edit recompute its own reference number without counting itself.
        Task<int> CountByBranchAndDateAsync(int branchId, DateTime countDate, int? excludeId = null);
        Task<bool> Save();
    }
}
