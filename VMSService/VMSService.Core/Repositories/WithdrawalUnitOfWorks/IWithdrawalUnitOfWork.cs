using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;

namespace VMSService.Core.Repositories.WithdrawalUnitOfWorks
{
    public interface IWithdrawalUnitOfWork : IAsyncDisposable
    {
        IWithdrawalRequestRepository Requests { get; }
        IWithdrawalRequestDenominationRepository Denominations { get; }
        IWithdrawalRequestAgentRepository WithdrawalAgents { get; }
        IWithdrawalRequestStatusHistoryRepository StatusHistory { get; }

        Task<int> SaveAsync(CancellationToken ct = default);
    }
}
