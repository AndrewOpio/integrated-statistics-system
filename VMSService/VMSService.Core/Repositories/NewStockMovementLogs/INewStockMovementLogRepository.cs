using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.NewStockMovementLogs
{
    public interface INewStockMovementLogRepository
    {
        Task<NewStockMovementlog> CreateAsync(NewStockMovementlog entity);
        Task<bool> CreateBatchAsync(List<NewStockMovementlog> entities);
        Task<NewStockMovementlog> FindByIdAsync(int id);
        Task<ICollection<NewStockMovementlog>> FindByStockMovementIdAsync(int stockMovementId);
        Task<bool> SaveAsync();
    }
}
