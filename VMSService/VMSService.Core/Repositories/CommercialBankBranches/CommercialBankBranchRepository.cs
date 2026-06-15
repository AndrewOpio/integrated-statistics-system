using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Configurations;

namespace VMSService.Core.Repositories.CommercialBankBranches
{
    public class CommercialBankBranchRepository(IConfigurationService _configService, ILogger<CommercialBankBranchRepository> _logger, 
        IHttpContextAccessor HttpContextAccessor) : ICommercialBankBranchRepository
    {
        //public async Task<ICollection<CommercialBankBranchDto>> FindByCommercialBankId(int bankId)
        //{
        //    try
        //    {
        //        return await _configService.GetAllBranches(bankId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving commercial bank branches");
        //        return new List<CommercialBankBranchDto>();
        //    }
        //}
    }
}