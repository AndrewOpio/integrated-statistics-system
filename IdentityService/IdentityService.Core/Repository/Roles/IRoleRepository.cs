using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Core.Repository.Roles
{
    public interface IRoleRepository: IRepositoryBase<Role, string>
    {
        Task<bool> Delete(string id);
    }
}
