using ConfigurationsService.Core.Repository.Common;
using ConfigurationsService.Entities;

namespace VMSService.Core.Repositories.Branches
{
    public interface IBranchRepository : IRepositoryBase<Branch, int>
    {
        Task<Branch> FindByName(string branchName);
    }
}