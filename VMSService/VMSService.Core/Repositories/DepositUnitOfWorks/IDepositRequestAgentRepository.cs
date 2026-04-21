using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositRequestAgentRepository
        : IRepositoryBase<DepositRequestAgent, Guid>
    {
        Task<IEnumerable<DepositRequestAgent>> GetByRequestAsync(Guid depositRequestId, CancellationToken ct = default);
        Task<IEnumerable<DepositRequestAgent>> GetByRequestAndSideAsync(Guid depositRequestId, string side, CancellationToken ct = default);
        void DeleteByRequestAndSide(Guid depositRequestId, string side);
    }
}