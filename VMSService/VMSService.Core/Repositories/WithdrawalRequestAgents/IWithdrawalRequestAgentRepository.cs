using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestAgents
{
    public interface IWithdrawalRequestAgentRepository
    : IRepositoryBase<WithdrawalRequestAgent, Guid>
    {
        Task<IEnumerable<WithdrawalRequestAgent>> GetByRequestAsync(Guid withdrawalRequestId, CancellationToken ct = default);
        Task<IEnumerable<WithdrawalRequestAgent>> GetByRequestAndSideAsync(Guid withdrawalRequestId, string side, CancellationToken ct = default);
        void DeleteByRequestAndSide(Guid withdrawalRequestId, string side);
    }
}
