using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.DenominationTypes
{
    public class DenominationTypeRepository(IStockService _stockService, ILogger<DenominationTypeRepository> _logger, VMSServiceDbContext _db) : IDenominationTypeRepository
    {
        public async Task<bool> Create(DenominationType model)
        {
            //_db.DenominationTypes.Add(model);
            //return await Save();

            try
            {
                return await _stockService.CreateDenominationType(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination type");
                return false;
            }
        }

        public async Task<ICollection<DenominationType>> FindAll()
        {
            //return await _db.DenominationTypes.Where(x => x.IsDeleted == false).ToListAsync();
            try
            {
                var denominationTypes = await _stockService.GetAllDenominationTypes();
                return denominationTypes.Where(x => x.IsDeleted == false).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all denomination types");
                return new List<DenominationType>();
            }
        }

        public async Task<DenominationType> FindById(int id)
        {
            //return await _db.DenominationTypes
            //    .FirstOrDefaultAsync(x => x.Id == id);

            try
            {
                var denomination = await _stockService.GetDenominationTypeById(id);
                if (denomination == null)
                    return null;

                return denomination;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination type with id {DenominationTypeId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            //return await _db.DenominationTypes
            //    .AnyAsync(x => x.Id == id);
            try
            {
                var denominationType = await FindById(id);
                return denominationType != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if denomination type exists with id {DenominationTypeId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            //return await _db.SaveChangesAsync() > 0;
            return true;
        }

        public async Task<bool> Update(DenominationType model)
        {
            //_db.DenominationTypes.Update(model);
            //return await Save();

            try
            {
                return await _stockService.UpdateDenominationType(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination type");
                return false;
            }
        }
    }
}