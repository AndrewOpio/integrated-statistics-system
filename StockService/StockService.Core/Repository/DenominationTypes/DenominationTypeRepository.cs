using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.DenominationTypes
{
    public class DenominationTypeRepository(ApplicationDbContext _db) : IDenominationTypeRepository
    {
        public async Task<ICollection<DenominationType>> FindAllAsync()
        {
            return await _db.DenominationTypes.Where(d => !d.IsDeleted).ToListAsync();
        }

        public async Task<DenominationType> FindByIdAsync(int id)
        {
            return await _db.DenominationTypes.Where(d => !d.IsDeleted)
                .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        public async Task<DenominationType> CreateAsync(DenominationType entity)
        {
            // Check for duplicate name
            var duplicate = await _db.DenominationTypes.AnyAsync(dt => dt.Name == entity.Name && !dt.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A denomination type with the same name already exists.");

            _db.DenominationTypes.Add(entity);
            await _db.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> UpdateAsync(DenominationType entity)
        {
            var existing = await _db.DenominationTypes.FindAsync(entity.Id);
            if (existing == null)
                return false;

            // Check for duplicate name (different id)
            var duplicate = await _db.DenominationTypes.AnyAsync(dt => dt.Name == entity.Name && dt.Id != entity.Id && !dt.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A denomination type with the same name already exists.");

            existing.Name = entity.Name;
            existing.IsDeleted = entity.IsDeleted;
            existing.ModifiedBy = entity.ModifiedBy;
            existing.ModifiedDate = entity.ModifiedDate;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
