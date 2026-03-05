using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.Branches
{
    public class BranchRepository(ApplicationDbContext _db) : IBranchRepository
    {
        public async Task<ICollection<Branch>> FindAllAsync()
        {
            return await _db.Branches.Where(b => !b.IsDeleted).ToListAsync();
        }

        public async Task<Branch> FindByIdAsync(int id)
        {
            return await _db.Branches.Where(b => !b.IsDeleted)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Branch> CreateAsync(Branch branch)
        {
            // Check for duplicate branch name
            var duplicate = await _db.Branches.AnyAsync(b => b.Name == branch.Name && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists.");

            _db.Branches.Add(branch);
            await _db.SaveChangesAsync();

            return branch;
        }

        public async Task<bool> UpdateAsync(Branch branch)
        {
            // Check for duplicate branch name (different id)
            var duplicate = await _db.Branches.AnyAsync(b => b.Name == branch.Name && b.Id != branch.Id && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists.");

            var existing = await _db.Branches.FindAsync(branch.Id);
            if (existing == null)
                return false;
                
            existing.Name = branch.Name;
            existing.IsDeleted = branch.IsDeleted;
            existing.ModifiedBy = branch.ModifiedBy;
            existing.ModifiedDate = branch.ModifiedDate;
            // Add other fields as needed
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
