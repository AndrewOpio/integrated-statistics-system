using IdentityService.Core.Repository.Common;
using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace IdentityService.Core.Repository.Roles
{
    public class RoleRepository(ApplicationDbContext _db, RoleManager<Role> _roleManager) : IRoleRepository
    {
        public async Task<bool> Create(Role model)
        {
            //model.Id = Guid.NewGuid().ToString();
            var result = await _roleManager.CreateAsync(model);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Role creation failed: {errors}");
            }
            if (result.Succeeded)
                return true;
            else
                return false;
        }

        public async Task<bool> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
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

        public async Task<ICollection<Role>> FindAll()
        {
            return await _roleManager.Roles.AsNoTracking().ToListAsync();
        }

        public async Task<Role> FindById(string id)
        {
            return await _roleManager.FindByIdAsync(id);
        }

        public async Task<bool> IsExists(string id)
        {
            return await _roleManager.Roles.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Role model)
        {
            var role = await _roleManager.FindByIdAsync(model.Id);

            if (role != null)
            {
                role.Name = model.Name;
                //role.Description = model.Description;
                //role.Permissions = model.Permissions;

                var result = await _roleManager.UpdateAsync(role);

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
