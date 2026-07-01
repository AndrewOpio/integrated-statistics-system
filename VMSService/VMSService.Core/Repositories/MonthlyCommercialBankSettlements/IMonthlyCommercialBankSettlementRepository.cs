using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyCommercialBankSettlements
{
    public interface IMonthlyCommercialBankSettlementRepository
    {
        Task<bool> Create(MonthlyCommercialBankSettlement entity);
        Task<ICollection<MonthlyCommercialBankSettlement>> FindAll();
        Task<MonthlyCommercialBankSettlement?> FindById(int id);
        Task<MonthlyCommercialBankSettlement?> FindByPeriodAndBank(DateTime period, int commercialBankId);
        Task<bool> IsExists(int id);
        Task<bool> Update(MonthlyCommercialBankSettlement entity);
        Task<bool> Save();
    }
}
