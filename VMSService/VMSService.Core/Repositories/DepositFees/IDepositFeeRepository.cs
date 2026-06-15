using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositFees
{
    public interface IDepositFeeRepository : IRepositoryBase<DepositFee, int>
    {
        Task<ICollection<DepositFee>> GetByDepositRequestId(Guid depositRequestId);
    }
}