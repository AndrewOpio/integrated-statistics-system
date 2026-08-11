using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingResults
{
    public interface IStockProcessingResultRepository : IRepositoryBase<StockProcessingResult, int>
    {
        Task<ICollection<StockProcessingResult>> FindBySessionIdAsync(int sessionId);

        /// <summary>Non-deleted results across multiple sessions in one query (verified and unverified alike).</summary>
        Task<ICollection<StockProcessingResult>> FindBySessionIdsAsync(IEnumerable<int> sessionIds);
    }
}
