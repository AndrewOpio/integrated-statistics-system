using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlySurpluses
{
    public interface IMonthlySurplusRepository : IRepositoryBase<MonthlySurplus, int>
    {
        Task<MonthlySurplus?> FindByBranchAndPeriod(int branchId, DateTime periodStart);
        Task<int> CountForPeriodAndBranch(int branchId, int year, int month);
    }
}
