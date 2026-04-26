using VMSService.Core.Repositories.DepositFees;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositUnitOfWork : IAsyncDisposable
    {
        IDepositRequestRepository Requests { get; }
        IDepositRequestDenominationRepository Denominations { get; }
        IDepositRequestAgentRepository DepositAgents { get; }
        IDepositRequestStatusHistoryRepository StatusHistory { get; }
        IDepositFeeRepository DepositFees { get; }

        Task<int> SaveAsync(CancellationToken ct = default);
    }
}