namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositUnitOfWork : IAsyncDisposable
    {
        IDepositRequestRepository Requests { get; }
        IDepositRequestDenominationRepository Denominations { get; }
        IDepositRequestAgentRepository DepositAgents { get; }
        IDepositRequestStatusHistoryRepository StatusHistory { get; }

        Task<int> SaveAsync(CancellationToken ct = default);
    }
}