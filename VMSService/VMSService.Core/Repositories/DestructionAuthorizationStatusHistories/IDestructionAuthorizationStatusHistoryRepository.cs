using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionAuthorizationStatusHistories
{
    public interface IDestructionAuthorizationStatusHistoryRepository : IRepositoryBase<DestructionAuthorizationStatusHistory, int>
    {
        Task<ICollection<DestructionAuthorizationStatusHistory>> FindByDestructionAuthorizationId(int authorizationId);
    }
}
