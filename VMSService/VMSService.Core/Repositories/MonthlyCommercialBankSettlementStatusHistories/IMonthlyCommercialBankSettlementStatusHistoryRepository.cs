using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories
{
    public interface IMonthlyCommercialBankSettlementStatusHistoryRepository
    {
        Task<bool> Create(MonthlyCommercialBankSettlementStatusHistory entity);
        Task<ICollection<MonthlyCommercialBankSettlementStatusHistory>> FindBySettlementId(int settlementId);
        Task<bool> Save();
    }
}
