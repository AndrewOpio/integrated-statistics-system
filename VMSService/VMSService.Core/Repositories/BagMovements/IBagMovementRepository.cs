using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BagMovements
{
    public interface IBagMovementRepository : IRepositoryBase<BagMovement, int>
    {
        Task<ICollection<BagMovement>> FindByBagIdAsync(int bagId);
        Task<bool> HasPendingForBag(int bagId, int pendingStatusId);
        Task<List<int>> GetBagIdsWithPendingMovement(int pendingStatusId);
    }
}
