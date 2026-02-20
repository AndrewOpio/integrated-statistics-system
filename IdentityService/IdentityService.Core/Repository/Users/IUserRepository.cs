using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.Users
{
    public interface IUserRepository : IRepositoryBase<User, string>
    {
        Task<User?> FindByName(string username);
    }
}
