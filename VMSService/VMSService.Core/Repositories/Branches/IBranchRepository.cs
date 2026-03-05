using VMSService.Core.Repository.Common;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Branches
{
    public interface IBranchRepository : IRepositoryBase<Branch, int>
    {
        Task<Branch> FindByName(string branchName);
    }
}