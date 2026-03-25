using ConfigurationsService.Dto;
using VMSService.Core.Repository.Common;

namespace VMSService.Core.Repositories.CommercialBanks
{
    public interface ICommercialBankRepository : IRepositoryBase<CommercialBankDto, int>
    {
    }
}