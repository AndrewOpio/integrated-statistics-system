using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Categories
{
    public class CategoryRepository(IStockService _stockService, ILogger<CategoryRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : ICategoryRepository
    {
        public async Task<bool> Create(Category model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return false;
            }
        }

        public async Task<ICollection<Category>> FindAll()
        {
            try
            {
                return await _stockService.GetAllCategories();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all categories");
                return new List<Category>();
            }
        }

        public async Task<Category> FindById(int id)
        {
            try
            {
                var category = await _stockService.GetCategoryById(id);
                if (category == null)
                    return null;

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with id {CategoryId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var category = await FindById(id);
                return category != null && !category.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if category exists with id {CategoryId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(Category model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return false;
            }
        }
    }
}