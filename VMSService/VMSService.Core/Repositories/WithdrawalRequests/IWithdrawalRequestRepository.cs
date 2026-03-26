using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequests
{
    public interface IWithdrawalRequestRepository : IRepositoryBase<WithdrawalRequest, Guid>
    {
        Task<WithdrawalRequest?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<WithdrawalRequest>> GetByCbAsync(int cbId, CancellationToken ct = default);
        Task<int> CountByCbBranchAndDateAsync(int cbId, int bouBranchId, DateOnly date, CancellationToken ct = default);
    }
}
