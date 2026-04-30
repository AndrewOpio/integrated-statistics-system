using ConfigurationsService.Dto;
using VMSService.Core.Repository.Common;

namespace VMSService.Core.Repositories.Branches
{
    public interface IBranchRepository : IRepositoryBase<BranchDto, int>
    {
        Task<BranchDto> FindByName(string branchName);
        Task<ICollection<BranchDto>> FindAll();
    }
}