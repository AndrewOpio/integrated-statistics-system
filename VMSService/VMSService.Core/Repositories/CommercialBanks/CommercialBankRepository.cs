using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.CommercialBanks
{
    public class CommercialBankRepository(IStockService _stockService, ILogger<CommercialBankRepository> _logger,
        IHttpContextAccessor _httpContextAccessor) : ICommercialBankRepository
    {
        public async Task<bool> Create(CommercialBank model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateCommercialBank(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commercial bank");
                return false;
            }
        }

        public async Task<ICollection<CommercialBank>> FindAll()
        {
            try
            {
                return await _stockService.GetAllCommercialBanks();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all commercial banks");
                return new List<CommercialBank>();
            }
        }

        public async Task<CommercialBank> FindById(int id)
        {
            try
            {
                var commercialBank = await _stockService.GetCommercialBankById(id);
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
                return commercialBank != null && !commercialBank.IsDeleted;
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

        public async Task<bool> Update(CommercialBank model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateCommercialBank(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commercial bank");
                return false;
            }
        }
    }
}