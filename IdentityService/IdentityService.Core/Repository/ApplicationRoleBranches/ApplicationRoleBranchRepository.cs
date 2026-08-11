using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Core.Repository.ApplicationRoleBranches
{
    public class ApplicationRoleBranchRepository(ApplicationDbContext _db) : IApplicationRoleBranchRepository
    {
        public async Task<ICollection<ApplicationRoleBranch>> FindAll()
        {
            return await _db.ApplicationRoleBranches
                .Include(x => x.ApplicationRole).ThenInclude(x => x.Application)
                .Include(x => x.ApplicationRole).ThenInclude(x => x.Role)
                .ToListAsync();
        }

        public async Task<ApplicationRoleBranch?> FindById(int id)
        {
            return await _db.ApplicationRoleBranches
                .Include(x => x.ApplicationRole)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<ApplicationRoleBranch>> FindByApplicationRoleId(int applicationRoleId)
        {
            return await _db.ApplicationRoleBranches
                .Where(x => x.ApplicationRoleId == applicationRoleId)
                .ToListAsync();
        }

        public async Task<ICollection<ApplicationRoleBranch>> FindByApplicationRoleIds(IEnumerable<int> applicationRoleIds)
        {
            return await _db.ApplicationRoleBranches
                .Where(x => applicationRoleIds.Contains(x.ApplicationRoleId))
                .ToListAsync();
        }

        public async Task<bool> IsExists(int applicationRoleId, int branchId)
        {
            return await _db.ApplicationRoleBranches
                .AnyAsync(x => x.ApplicationRoleId == applicationRoleId && x.BranchId == branchId);
        }

        public async Task<bool> Create(ApplicationRoleBranch entity)
        {
            _db.ApplicationRoleBranches.Add(entity);
            return await Save();
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await FindById(id);
            if (entity == null) return false;
            _db.ApplicationRoleBranches.Remove(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
