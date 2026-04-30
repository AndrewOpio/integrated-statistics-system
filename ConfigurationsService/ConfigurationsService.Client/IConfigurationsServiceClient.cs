using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationsService.Client
{
    public interface IConfigurationsServiceClient
    {
        Task<IEnumerable<CommercialBankBranchDto>> GetCbBranchesByCbIdAsync(int id);
        Task<CommercialBankBranchDto> GetCbBranchByIdAsync(int id);
        Task<IEnumerable<BranchDto>> GetAllBouBranchesAsync();
        Task<BranchDto> GetBouBranchByIdAsync(int id);
        Task<IEnumerable<CommercialBankDto>> GetAllCommercialBankAsync();
        Task<CommercialBankDto> GetCbIdAsync(int id);
    }
}
