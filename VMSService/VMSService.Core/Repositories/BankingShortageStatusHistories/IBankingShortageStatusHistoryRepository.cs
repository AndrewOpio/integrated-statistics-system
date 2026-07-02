using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingShortageStatusHistories
{
    public interface IBankingShortageStatusHistoryRepository : IRepositoryBase<BankingShortageStatusHistory, int>
    {
        Task<ICollection<BankingShortageStatusHistory>> FindByBankingShortageId(int bankingShortageId);
    }
}
