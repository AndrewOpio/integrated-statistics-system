using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortages
{
    public interface IMonthlyShortageDetailRepository
    {
        Task<ICollection<MonthlyShortageDetail>> FindByMonthlyShortageId(int monthlyShortageId);
        Task<MonthlyShortageDetail?> FindByMonthlyShortageIdAndBranch(int monthlyShortageId, int branchId);
        Task<bool> Create(MonthlyShortageDetail entity);
        Task<bool> Update(MonthlyShortageDetail entity);
        Task<bool> DeleteByMonthlyShortageId(int monthlyShortageId);
    }
}
