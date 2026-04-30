using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequestStatusHistories
{
    public interface IWithdrawalRequestStatusHistoryRepository
    : IRepositoryBase<WithdrawalRequestStatusHistory, Guid>
    {
        Task<IEnumerable<WithdrawalRequestStatusHistory>> GetByRequestAsync(Guid requestId, CancellationToken ct = default);
        Task<WithdrawalRequestStatusHistory> FindByNewStatusId(int newStatusId);
    }
}
