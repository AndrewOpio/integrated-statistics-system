using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositRequestDenominationRepository
        : IRepositoryBase<DepositRequestDenomination, Guid>
    {
        Task<IEnumerable<DepositRequestDenomination>> GetByRequestAsync(Guid depositRequestId, CancellationToken ct = default);
        Task<IEnumerable<DepositRequestDenomination>> GetByRequestAndOriginAsync(Guid depositRequestId, string origin, CancellationToken ct = default);
        void DeleteByRequest(Guid depositRequestId, string origin);
        Task CopyToBouAsync(Guid requestId, string userId, CancellationToken ct);
    }
}