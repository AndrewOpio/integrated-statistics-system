using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.SubCategories
{
    public class SubCategoryRepository(IStockService _stockService, ILogger<SubCategoryRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : ISubCategoryRepository
    {
        public async Task<bool> Create(SubCategory model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateSubCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub category");
                return false;
            }
        }

        public async Task<ICollection<SubCategory>> FindAll()
        {
            try
            {
                return await _stockService.GetAllSubCategories();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sub categories");
                return new List<SubCategory>();
            }
        }

        public async Task<SubCategory> FindById(int id)
        {
            try
            {
                var subCategory = await _stockService.GetSubCategoryById(id);
                if (subCategory == null)
                    return null;

                return subCategory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sub category with id {SubCategoryId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var subCategory = await FindById(id);
                return subCategory != null && !subCategory.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if sub category exists with id {SubCategoryId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(SubCategory model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateSubCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub category");
                return false;
            }
        }
    }
}