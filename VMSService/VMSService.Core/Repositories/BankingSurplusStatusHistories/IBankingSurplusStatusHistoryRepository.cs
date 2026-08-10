using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingSurplusStatusHistories
{
    public interface IBankingSurplusStatusHistoryRepository : IRepositoryBase<BankingSurplusStatusHistory, int>
    {
        Task<ICollection<BankingSurplusStatusHistory>> FindByBankingSurplusId(int bankingSurplusId);
    }
}
