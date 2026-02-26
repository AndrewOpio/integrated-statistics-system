using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.Categories
{
    public class CategoryRepository(ApplicationDbContext _db) : ICategoryRepository
    {

        public async Task<ICollection<Category>> FindAllAsync()
        {
            return await _db.Categories.ToListAsync();
        }

        public async Task<Category> FindByIdAsync(int id)
        {
            return await _db.Categories.FindAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            // Check for duplicate category name
            var duplicate = await _db.Categories.AnyAsync(c => c.Name == category.Name);
            if (duplicate)
                throw new InvalidOperationException("A category with the same name already exists.");

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return category;
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            var existing = await _db.Categories.FindAsync(category.Id);
            if (existing == null)
                return false;

            // Check for duplicate category name (different id)
            var duplicate = await _db.Categories.AnyAsync(c => c.Name == category.Name && c.Id != category.Id);
            if (duplicate)
                throw new InvalidOperationException("A category with the same name already exists.");

            existing.Name = category.Name;
            existing.Classification = category.Classification;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
