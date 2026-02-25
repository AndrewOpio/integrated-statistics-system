using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.BranchCategories
{
    public class BranchCategoryRepository(ApplicationDbContext _db) : IBranchCategoryRepository
    {
        public async Task<ICollection<BranchCategory>> FindAllAsync()
        {
            return await _db.BranchCategories
                .Include(bc => bc.Branch)
                .Include(bc => bc.Category)
                .ToListAsync();
        }

        public async Task<BranchCategory> FindByIdAsync(int id)
        {
            return await _db.BranchCategories
                    .Include(bc => bc.Branch)
                    .Include(bc => bc.Category)
                    .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        public async Task<BranchCategory> CreateAsync(BranchCategory entity)
        {
            // Check for duplicate BranchId and CategoryId combination
            var duplicate = await _db.BranchCategories.AnyAsync(bc => bc.BranchId == entity.BranchId && bc.CategoryId == entity.CategoryId);
            if (duplicate)
                throw new InvalidOperationException("A branch category with the same branch and category already exists.");

            _db.BranchCategories.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(BranchCategory entity)
        {
            var existing = await _db.BranchCategories.FindAsync(entity.Id);
            if (existing == null)
                return false;

            // Check for duplicate BranchId and CategoryId combination (different Id)
            var duplicate = await _db.BranchCategories.AnyAsync(bc => bc.BranchId == entity.BranchId && bc.CategoryId == entity.CategoryId && bc.Id != entity.Id);
            if (duplicate)
                throw new InvalidOperationException("A branch category with the same branch and category already exists.");

            existing.BranchId = entity.BranchId;
            existing.CategoryId = entity.CategoryId;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
