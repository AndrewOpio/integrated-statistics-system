using ConfigurationsService.Core.Repository.Common;
using ConfigurationsService.Data.Entities;

namespace ConfigurationsService.Core.Repositories.CommercialBankBranches
{
    public interface ICommercialBankBranchRepository : IRepositoryBase<CommercialBankBranch, int>
    {
        Task<ICollection<CommercialBankBranch>> FindByCommercialBankId(int bankId);
        Task<ICollection<CommercialBankBranch>> FindByDistrictId(int districtId);
    }
}