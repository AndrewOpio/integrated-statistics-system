using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.BranchCategories
{
    public class BranchCategoryRepository(IStockService _stockService, ILogger<BranchCategoryRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : IBranchCategoryRepository
    {
        public async Task<bool> Create(BranchCategory model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateBranchCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch category");
                return false;
            }
        }

        public async Task<ICollection<BranchCategory>> FindAll()
        {
            try
            {
                return await _stockService.GetAllBranchCategories();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all branch categories");
                return new List<BranchCategory>();
            }
        }

        public async Task<BranchCategory> FindById(int id)
        {
            try
            {
                var branchCategory = await _stockService.GetBranchCategoryById(id);
                if (branchCategory == null)
                    return null;

                return branchCategory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch category with id {BranchCategoryId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var branchCategory = await FindById(id);
                return branchCategory != null && !branchCategory.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if branch category exists with id {BranchCategoryId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(BranchCategory model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateBranchCategory(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch category");
                return false;
            }
        }

        public async Task<BranchCategory> FindByBranchAndCategoryName(int branchId, string categoryName)
        {
            //var branchCategories = await FindAll();
            //var filtered = branchCategories.Where(bc => bc.Branch.Id == branchId && bc.Category.Name == categoryName).FirstOrDefault();
            //return filtered;
            return (await FindAll()).Where(bc => bc.BranchId == branchId && bc.Category.Name == categoryName).FirstOrDefault();
        }
    }
}