using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionRequests
{
    public interface IDestructionRequestRepository : IRepositoryBase<DestructionRequest, int>
    {
        Task<ICollection<DestructionType>> GetAllDestructionTypes();
        Task<bool> UpdateWithChildren(DestructionRequest entity);
        Task<bool> UpdateDenominationDestroyed(int denominationId, decimal destroyed);
    }
}
