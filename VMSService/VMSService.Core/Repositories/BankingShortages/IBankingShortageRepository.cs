using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingShortages
{
    public interface IBankingShortageRepository : IRepositoryBase<BankingShortage, int>
    {
        Task<BankingShortage?> FindByPeriod(DateTime period);
    }
}
