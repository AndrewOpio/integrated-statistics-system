using VMSService.Core.Repository.Common;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.BranchCategories
{
    public interface IBranchCategoryRepository : IRepositoryBase<BranchCategory, int>
    {
        Task<BranchCategory> FindByBranchAndCategoryName(int branchId, string categoryName);
        Task<BranchCategory> FindByCategoryName(string categoryName);
    }
}