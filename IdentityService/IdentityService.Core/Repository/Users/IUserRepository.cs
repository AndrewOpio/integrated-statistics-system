using IdentityService.Core.Models;
using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Core.Repository.Users
{
    public interface IUserRepository : IRepositoryBase<User, string>
    {
        Task<User?> FindByName(string username);
        Task<List<NonStaffUserViewModel>> GetNonStaffUserList();
        Task<IdentityResult> Create(User model, string password);
        Task<IdentityResult> ResetPassword(string userId, string newPassword);
        Task<IdentityResult> ChangePassword(string userId, string currentPassword, string newPassword);
    }
}
