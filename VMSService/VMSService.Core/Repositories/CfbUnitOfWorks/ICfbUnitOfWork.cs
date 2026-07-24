using Microsoft.EntityFrameworkCore.Storage;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbTopupRequests;

namespace VMSService.Core.Repositories.CfbUnitOfWorks
{
    public interface ICfbUnitOfWork : IAsyncDisposable
    {
        ITopupRequestRepository Requests { get; }
        ICfbCashHoldingRepository Holdings { get; }
        Task<int> SaveAsync(CancellationToken ct = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    }
}
