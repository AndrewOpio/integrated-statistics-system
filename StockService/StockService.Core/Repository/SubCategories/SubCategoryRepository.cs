using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.SubCategories
{
    public class SubCategoryRepository(ApplicationDbContext _db) : ISubCategoryRepository
    {

        public async Task<ICollection<SubCategory>> FindAllAsync()
        {
            return await _db.SubCategories
                .Include(sc => sc.Category)
                .Where(sc => !sc.IsDeleted)
                .ToListAsync();
        }

        public async Task<SubCategory> FindByIdAsync(int id)
        {
            return await _db.SubCategories
                .Include(sc => sc.Category)
                .Where(sc => !sc.IsDeleted)
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<SubCategory> CreateAsync(SubCategory subCategory)
        {
            // Check for duplicate subcategory name within the same category
            var duplicate = await _db.SubCategories
                .AnyAsync(sc => sc.Name == subCategory.Name 
                    && sc.CategoryId == subCategory.CategoryId 
                    && !sc.IsDeleted);
            
            if (duplicate)
                throw new InvalidOperationException("A subcategory with the same name already exists in this category.");

            _db.SubCategories.Add(subCategory);
            await _db.SaveChangesAsync();

            return subCategory;
        }

        public async Task<bool> UpdateAsync(SubCategory subCategory)
        {
            var existing = await _db.SubCategories.FindAsync(subCategory.Id);
            if (existing == null)
                return false;

            // Check for duplicate subcategory name within the same category (different id)
            var duplicate = await _db.SubCategories
                .AnyAsync(sc => sc.Name == subCategory.Name 
                    && sc.CategoryId == subCategory.CategoryId 
                    && sc.Id != subCategory.Id 
                    && !sc.IsDeleted);
            
            if (duplicate)
                throw new InvalidOperationException("A subcategory with the same name already exists in this category.");

            existing.Name = subCategory.Name;
            existing.CategoryId = subCategory.CategoryId;
            existing.IsDeleted = subCategory.IsDeleted;
            existing.ModifiedBy = subCategory.ModifiedBy;
            existing.ModifiedDate = subCategory.ModifiedDate;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}