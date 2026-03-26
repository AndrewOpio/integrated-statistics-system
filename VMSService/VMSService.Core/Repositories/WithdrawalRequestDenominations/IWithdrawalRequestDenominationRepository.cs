using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
