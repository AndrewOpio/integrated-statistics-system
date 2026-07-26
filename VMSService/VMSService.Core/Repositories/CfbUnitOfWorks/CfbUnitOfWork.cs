using Microsoft.EntityFrameworkCore.Storage;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbTopupRequests;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.CfbUnitOfWorks
{
    public class CfbUnitOfWork(
        VMSServiceDbContext _db,
        ICfbCashHoldingRepository _holdingRepo,
        ITopupRequestRepository _topupRepo) : ICfbUnitOfWork
    {
        public ITopupRequestRepository Requests => _topupRepo;
        public ICfbCashHoldingRepository Holdings => _holdingRepo;

        public async Task<int> SaveAsync(CancellationToken ct = default)
            => await _db.SaveChangesAsync(ct);

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
            => await _db.Database.BeginTransactionAsync(ct);

        public async ValueTask DisposeAsync()
            => await _db.DisposeAsync();
    }
}
