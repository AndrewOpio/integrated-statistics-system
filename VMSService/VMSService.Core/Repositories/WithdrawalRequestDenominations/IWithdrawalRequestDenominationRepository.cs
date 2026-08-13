using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.BoxStates;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestDenominations
{
    public interface IWithdrawalRequestDenominationRepository
    : IRepositoryBase<WithdrawalRequestDenomination, Guid>
    {
        Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAsync(Guid withdrawalRequestId, CancellationToken ct = default);
        Task<IEnumerable<WithdrawalRequestDenomination>> GetByRequestAndOriginAsync(Guid withdrawalRequestId, string origin, CancellationToken ct = default);
        void DeleteByRequest(Guid withdrawalRequestId, string origin);

        //Task CopyToBouAsync(Guid requestId, string userId, CancellationToken ct);
        Task<bool> HasBouOriginAsync(Guid requestId, CancellationToken ct = default);
        Task<HashSet<Guid>> GetRequestIdsWithBouOriginAsync(IEnumerable<Guid> requestIds, CancellationToken ct = default);
        Task DeleteBouRowsWithChildrenAsync(Guid requestId, CancellationToken ct = default);
        Task<List<AvailableBoxDto>> GetAvailableBoxesAsync(int denominationId, int branchId, Guid? excludeRequestId, CancellationToken ct = default);
        
        // In IWithdrawalRequestDenominationRepository
        Task<List<HeldByBoxResult>> GetHeldByBoxAsync(Guid requestId, CancellationToken ct = default);

    }

    public record HeldByBoxResult(string CaseNumber, int DenominationId, int BundlesHeld, long MinSerialStartHeld);
}
