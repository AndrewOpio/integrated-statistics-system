using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortages
{
    public interface IMonthlyShortageRepository : IRepositoryBase<MonthlyShortage, int>
    {
        Task<MonthlyShortage?> FindByPeriod(DateTime period);
        Task<int> CountForPeriod(int year, int month);
    }
}
