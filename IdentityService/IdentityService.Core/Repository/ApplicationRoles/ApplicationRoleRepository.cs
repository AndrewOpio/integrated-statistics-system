using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Core.Repository.ApplicationRoles
{
    public class ApplicationRoleRepository(ApplicationDbContext _db) : IApplicationRoleRepository
    {
        public async Task<bool> Create(ApplicationRole model)
        {
            _db.ApplicationRoles.Add(model);
            return await Save();
        }

        public async Task<bool> Delete(int id)
        {
            var model = await FindById(id);
            _db.ApplicationRoles.Remove(model);
            return await Save();
        }

        public async Task<ICollection<ApplicationRole>> FindAll()
        {
            return await _db.ApplicationRoles
                .Include(x => x.Application)
                .Include(x => x.Role)
                .ToListAsync();
        }

        public async Task<ApplicationRole> FindById(int id)
        {
            var model = await _db.ApplicationRoles
                .Include(x => x.Application)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id);
            return model;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ApplicationRoles
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int applicationId, string roleId)
        {
            return await _db.ApplicationRoles
                .AnyAsync(x => x.ApplicationId == applicationId && x.RoleId == roleId);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ApplicationRole model)
        {
            _db.ApplicationRoles.Update(model);
            return await Save();
        }
    }
}
