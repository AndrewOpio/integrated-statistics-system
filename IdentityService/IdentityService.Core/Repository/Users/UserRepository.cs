using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Core.Repository.Users
{
    public class UserRepository(ApplicationDbContext _db, UserManager<User> _userManager, ILogger<UserRepository> _logger) : IUserRepository
    {
        public async Task<bool> Create(User model)
        {
            var result = await _userManager.CreateAsync(model);
            if (result.Succeeded)
                return true;
            else
                return false;
        }

        public async Task<ICollection<User>> FindAll()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<User> FindById(string id)
        {
            return await _userManager.FindByIdAsync(id);
        }

        public async Task<User?> FindByName(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        public async Task<bool> IsExists(string id)
        {
            return await _userManager.Users.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(User model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user != null)
            {
                user.UserName = model.UserName;
                user.IsActive = model.IsActive;
                user.DisableDate = model.DisableDate;
                user.EndDate = model.EndDate;
                user.Title = model.Title;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
    }
}
