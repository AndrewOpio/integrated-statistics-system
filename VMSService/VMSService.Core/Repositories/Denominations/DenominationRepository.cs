using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.Denominations
{
    public class DenominationRepository(IStockService _stockService, ILogger<DenominationRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : IDenominationRepository
    {
        public async Task<bool> Create(Denomination model)
        {
            //_db.Denominations.Add(model);
            //return await Save();

            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateDenomination(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination");
                return false;
            }
        }

        public async Task<ICollection<Denomination>> FindAll()
        {
            //return await _db.Denominations.Where(x => x.IsDeleted == false)
            //    .ToListAsync();

            try
            {
                return await _stockService.GetAllDenominations();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all denominations");
                return new List<Denomination>();
            }
        }

        public async Task<Denomination> FindById(int id)
        {
            //return await _db.Denominations
            //    .FirstOrDefaultAsync(x => x.Id == id);

            try
            {
                var denomination = await _stockService.GetDenominationById(id);
                if (denomination == null)
                    return null;

                return denomination;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination with id {DenominationId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            //return await _db.Denominations
            //    .AnyAsync(x => x.Id == id);

            try
            {
                var denomination = await FindById(id);
                return denomination != null && !denomination.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if denomination exists with id {DenominationId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            //return await _db.SaveChangesAsync() > 0;
            return true;
        }

        public async Task<bool> Update(Denomination model)
        {
            //_db.Denominations.Update(model);
            //return await Save();

            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateDenomination(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination");
                return false;
            }
        }
    }
}