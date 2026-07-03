using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingResults
{
    public interface IStockProcessingResultRepository : IRepositoryBase<StockProcessingResult, int>
    {
        Task<ICollection<StockProcessingResult>> FindBySessionIdAsync(int sessionId);
    }
}
