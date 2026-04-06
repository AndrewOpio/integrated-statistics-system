using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositRequestStatusHistoryRepository
        : IRepositoryBase<DepositRequestStatusHistory, Guid>
    {
        Task<IEnumerable<DepositRequestStatusHistory>> GetByRequestAsync(Guid depositRequestId, CancellationToken ct = default);
    }
}