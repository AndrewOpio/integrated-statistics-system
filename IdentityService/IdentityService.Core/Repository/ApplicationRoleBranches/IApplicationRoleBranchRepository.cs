using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.ApplicationRoleBranches
{
    public interface IApplicationRoleBranchRepository
    {
        Task<ICollection<ApplicationRoleBranch>> FindAll();
        Task<ApplicationRoleBranch?> FindById(int id);
        Task<ICollection<ApplicationRoleBranch>> FindByApplicationRoleId(int applicationRoleId);
        Task<ICollection<ApplicationRoleBranch>> FindByApplicationRoleIds(IEnumerable<int> applicationRoleIds);
        Task<bool> IsExists(int applicationRoleId, int branchId);
        Task<bool> Create(ApplicationRoleBranch entity);
        Task<bool> Delete(int id);
        Task<bool> Save();
    }
}
