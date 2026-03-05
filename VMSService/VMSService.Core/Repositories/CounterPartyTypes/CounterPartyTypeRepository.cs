using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.CounterPartyTypes
{
    public class CounterPartyTypeRepository(IStockService _stockService, ILogger<CounterPartyTypeRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : ICounterPartyTypeRepository
    {
        public async Task<bool> Create(CounterPartyType model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateCounterPartyType(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating counter party type");
                return false;
            }
        }

        public async Task<ICollection<CounterPartyType>> FindAll()
        {
            try
            {
                return await _stockService.GetAllCounterPartyTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all counter party types");
                return new List<CounterPartyType>();
            }
        }

        public async Task<CounterPartyType> FindById(int id)
        {
            try
            {
                var counterPartyType = await _stockService.GetCounterPartyTypeById(id);
                if (counterPartyType == null)
                    return null;

                return counterPartyType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type with id {CounterPartyTypeId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var counterPartyType = await FindById(id);
                return counterPartyType != null && !counterPartyType.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if counter party type exists with id {CounterPartyTypeId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(CounterPartyType model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateCounterPartyType(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating counter party type");
                return false;
            }
        }

        public async Task<CounterPartyType> FindByName(string name)
        {
            return (await FindAll()).Where(t => t.Name == name).FirstOrDefault();
        }
    }
}