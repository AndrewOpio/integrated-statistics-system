using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BagMovementStatusHistories
{
    public interface IBagMovementStatusHistoryRepository : IRepositoryBase<BagMovementStatusHistory, int>
    {
        Task<ICollection<BagMovementStatusHistory>> FindByBagMovementId(int bagMovementId);
    }
}
