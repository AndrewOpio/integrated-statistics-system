using ConfigurationsService.Core.Repository.Common;
using ConfigurationsService.Data.Entities;

namespace ConfigurationsService.Core.Repositories.CommercialBanks
{
    public interface ICommercialBankRepository : IRepositoryBase<CommercialBank, int>
    {
        Task<CommercialBank> FindByName(string bankName);
        Task<CommercialBank> FindByCode(string code);
    }
}