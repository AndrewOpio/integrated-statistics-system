using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingSurpluses
{
    public interface IBankingSurplusRepository : IRepositoryBase<BankingSurplus, int>
    {
        Task<BankingSurplus?> FindByBranchAndPeriod(int branchId, DateTime periodStart);
    }
}
