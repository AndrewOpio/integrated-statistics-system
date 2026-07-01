using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionRequestStatusHistories
{
    public interface IDestructionRequestStatusHistoryRepository : IRepositoryBase<DestructionRequestStatusHistory, int>
    {
        Task<ICollection<DestructionRequestStatusHistory>> FindByDestructionRequestId(int destructionRequestId);
    }
}
