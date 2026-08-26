using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Core.Repository.UserBranches
{
    public class UserBranchRepository(ApplicationDbContext _db) : IUserBranchRepository
    {
        public async Task<ICollection<UserBranch>> FindAll()
        {
            return await _db.UserBranches
                .Include(x => x.User)
                .ToListAsync();
        }

        public async Task<UserBranch?> FindById(int id)
        {
            return await _db.UserBranches
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<UserBranch>> FindByUserId(string userId)
        {
            return await _db.UserBranches
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> IsExists(string userId, int branchId)
        {
            return await _db.UserBranches
                .AnyAsync(x => x.UserId == userId && x.BranchId == branchId);
        }

        public async Task<bool> Create(UserBranch entity)
        {
            _db.UserBranches.Add(entity);
            return await Save();
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _db.UserBranches.FindAsync(id);
            if (entity == null) return false;
            _db.UserBranches.Remove(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
