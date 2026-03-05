using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Branches
{
    public class BranchRepository(IStockService _stockService, ILogger<BranchRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : IBranchRepository
    {
        public async Task<bool> Create(Branch model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                model.CreatedBy = user;
                model.ModifiedBy = user;
                return await _stockService.CreateBranch(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                return false;
            }
        }

        public async Task<ICollection<Branch>> FindAll()
        {
            try
            {
                return await _stockService.GetAllBranches();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all branches");
                return new List<Branch>();
            }
        }

        public async Task<Branch> FindById(int id)
        {
            try
            {
                var branch = await _stockService.GetBranchById(id);
                if (branch == null)
                    return null;

                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch with id {BranchId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var branch = await FindById(id);
                return branch != null && !branch.IsDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if branch exists with id {BranchId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(Branch model)
        {
            try
            {
                var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "unknown";
                var now = DateTime.Now;
                model.ModifiedBy = user;
                model.ModifiedDate = now;
                return await _stockService.UpdateBranch(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch");
                return false;
            }
        }

        public async Task<Branch> FindByName(string branchName)
        {
            return (await FindAll()).Where(b => b.Name == branchName).FirstOrDefault();
        }
    }
}