using ConfigurationsService.Dto;

namespace VMSService.Core.Services.Configurations
{
    public interface IConfigurationService
    {
        // Branch methods
        Task<ICollection<BranchDto>> GetAllBranches();
        Task<BranchDto> GetBranchById(int id);

        // CommercialBank methods
        Task<ICollection<CommercialBankDto>> GetAllCommercialBanks();
        Task<CommercialBankDto> GetCommercialBankById(int id);

        //Task<ICollection<CommercialBankBranchDto>> GetCbBranchesByCommercialBankId(int id);
    }
}