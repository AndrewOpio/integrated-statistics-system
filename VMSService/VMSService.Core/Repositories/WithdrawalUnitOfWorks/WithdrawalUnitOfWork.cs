using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repositories.BoxStates;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.WithdrawalUnitOfWorks
{
    public class WithdrawalUnitOfWork(VMSServiceDbContext _db) : IWithdrawalUnitOfWork
    {
        public IWithdrawalRequestRepository Requests { get; }
            = new WithdrawalRequestRepository(_db);

        public IWithdrawalRequestDenominationRepository Denominations { get; }
            = new WithdrawalRequestDenominationRepository(_db);

        public IWithdrawalRequestAgentRepository WithdrawalAgents { get; }
            = new WithdrawalRequestAgentRepository(_db);

        public IWithdrawalRequestStatusHistoryRepository StatusHistory { get; }
            = new WithdrawalRequestStatusHistoryRepository(_db);

        public IBoxStateRepository BoxStates { get; } 
            = new BoxStateRepository(_db);

        public async Task<int> SaveAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
            => await _db.Database.BeginTransactionAsync(ct);
        public async ValueTask DisposeAsync()
            => await _db.DisposeAsync();
    }
}
