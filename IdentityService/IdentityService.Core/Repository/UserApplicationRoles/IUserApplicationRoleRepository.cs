using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.UserApplicationRoles
{
    public interface IUserApplicationRoleRepository : IRepositoryBase<UserApplicationRole, int>
    {
        Task<bool> Delete(int id);
        Task<ICollection<UserApplicationRole>> FindByUserId(string userId);
        Task<ICollection<UserApplicationRole>> FindByUserName(string username);
    }
}
