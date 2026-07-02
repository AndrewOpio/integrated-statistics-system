using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Core.Repository.UserApplicationRoles
{
    public class UserApplicationRoleRepository(ApplicationDbContext _db) : IUserApplicationRoleRepository
    {
        public async Task<bool> Create(UserApplicationRole model)
        {
            _db.UserApplicationRoles.Add(model);
            return await Save();
        }

        public async Task<bool> Delete(int id)
        {
            var model = await FindById(id);
            _db.UserApplicationRoles.Remove(model);
            return await Save();
        }

        public async Task<ICollection<UserApplicationRole>> FindAll()
        {
            return await _db.UserApplicationRoles.ToListAsync();
        }

        public async Task<UserApplicationRole> FindById(int id)
        {
            var applicationrole = await _db.UserApplicationRoles
                .FirstOrDefaultAsync(x => x.Id == id);
            return applicationrole;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.UserApplicationRoles
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(UserApplicationRole model)
        {
            _db.UserApplicationRoles.Update(model);
            return await Save();
        }

        public async Task<ICollection<UserApplicationRole>> FindByUserId(string userId)
        {
            return await _db.UserApplicationRoles
                .Where(x => x.UserId == userId)
                .Include(x => x.ApplicationRole)
                .Include(x => x.ApplicationRole.Role)
                .Include(x => x.ApplicationRole.Application)
                .ToListAsync();
        }

        public async Task<ICollection<UserApplicationRole>> FindByUserName(string username)
        {
            return await _db.UserApplicationRoles
                .Where(x => x.User.UserName == username)
                .Include(x => x.ApplicationRole)
                .Include(x => x.ApplicationRole.Role)
                .Include(x => x.ApplicationRole.Application)
                .ToListAsync();
        }

        public async Task<ICollection<UserApplicationRole>> GetUsersByRoleIds(List<int> appRoleIds)
        {
            return await _db.UserApplicationRoles
                .Where(x => appRoleIds.Contains(x.ApplicationRoleId))
                .Include(x => x.User)
                .ToListAsync();
        }

        public async Task<ICollection<UserApplicationRole>> GetUsersByRoleName(string roleName)
        {
            return await _db.UserApplicationRoles
                .Where(x => x.ApplicationRole.Role.Name == roleName)
                .Include(x => x.User)
                .Include(x => x.ApplicationRole)
                    .ThenInclude(x => x.Role)
                .ToListAsync();
        }
    }
}
