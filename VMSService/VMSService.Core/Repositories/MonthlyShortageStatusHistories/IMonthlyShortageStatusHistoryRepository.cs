using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageStatusHistories
{
    public interface IMonthlyShortageStatusHistoryRepository : IRepositoryBase<MonthlyShortageStatusHistory, int>
    {
        Task<ICollection<MonthlyShortageStatusHistory>> FindByMonthlyShortageId(int monthlyShortageId);
    }
}
