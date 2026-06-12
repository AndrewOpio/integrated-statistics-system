using ConfigurationsService.Client;

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

        // CommercialBankBranch methods
        Task<ICollection<CommercialBankBranchDto>> GetAllCommercialBankBranches();
        Task<CommercialBankBranchDto> GetCommercialBankBranchById(int id);
        Task<ICollection<CommercialBankBranchDto>> GetCommercialBankBranchesByBankId(int commercialBankId);
    }
}