using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public class DepositUnitOfWork(VMSServiceDbContext _db) : IDepositUnitOfWork
    {
        public IDepositRequestRepository Requests { get; }
            = new DepositRequestRepository(_db);

        public IDepositRequestDenominationRepository Denominations { get; }
            = new DepositRequestDenominationRepository(_db);

        public IDepositRequestAgentRepository DepositAgents { get; }
            = new DepositRequestAgentRepository(_db);

        public IDepositRequestStatusHistoryRepository StatusHistory { get; }
            = new DepositRequestStatusHistoryRepository(_db);

        public async Task<int> SaveAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);

        public async ValueTask DisposeAsync()
            => await _db.DisposeAsync();
    }
}