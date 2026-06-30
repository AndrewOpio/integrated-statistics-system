using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockProcessingSessions
{
    public interface IStockProcessingSessionRepository : IRepositoryBase<StockProcessingSession, int>
    {
        Task<List<StockProcessingSession>> GetByPlanIdAsync(int planId);
    }
}
