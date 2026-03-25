using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.ApplicationRoles
{
    public interface IApplicationRoleRepository : IRepositoryBase<ApplicationRole, int>
    {
        Task<bool> Delete(int id);
        Task<bool> IsExists(int applicationId, string roleId);
        Task<List<ApplicationRole>> FindByApplicationId(int applicationId);
    }
}
