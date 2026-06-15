using ConfigurationsService.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Repository.Common;
using VMSService.Core.Services.Configurations;

namespace VMSService.Core.Repositories.CommercialBanks
{
    public class CommercialBankRepository(IConfigurationService _configService, ILogger<CommercialBankRepository> _logger,
        IHttpContextAccessor _httpContextAccessor) : ICommercialBankRepository
    {
        public Task<bool> Create(CommercialBankDto entity)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<CommercialBankDto>> FindAll()
        {
            try
            {
                return await _configService.GetAllCommercialBanks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all commercial banks");
                return new List<CommercialBankDto>();
            }
        }

        public async Task<CommercialBankDto> FindById(int id)
        {
            try
            {
                var commercialBank = await _configService.GetCommercialBankById(id);
                if (commercialBank == null)
                    return null;

                return commercialBank;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commercial bank with id {CommercialBankId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var commercialBank = await FindById(id);
                return commercialBank != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if commercial bank exists with id {CommercialBankId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public Task<bool> Update(CommercialBankDto entity)
        {
            throw new NotImplementedException();
        }
    }
}