using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.Denominations
{
    public class DenominationRepository(ApplicationDbContext _db) : IDenominationRepository
    {

        public async Task<ICollection<Denomination>> FindAllAsync()
        {
            return await _db.Denominations.Where(d => !d.IsDeleted)
                .Include(d => d.DenominationType)
                .ToListAsync();
        }

        public async Task<Denomination> FindByIdAsync(int id)
        {
            return await _db.Denominations.Where(d => !d.IsDeleted)
                .Include(d => d.DenominationType)
                .FirstOrDefaultAsync(bc => bc.Id == id);
        }

        public async Task<Denomination> CreateAsync(Denomination entity)
        {
            // Check for duplicate name
            var duplicate = await _db.Denominations.AnyAsync(d => d.Name == entity.Name && !d.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A denomination with the same name already exists.");

            _db.Denominations.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Denomination entity)
        {
            var existing = await _db.Denominations.FindAsync(entity.Id);
            if (existing == null)
                return false;

            // Check for duplicate name (different id)
            var duplicate = await _db.Denominations.AnyAsync(d => d.Name == entity.Name && d.Id != entity.Id && !d.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A denomination with the same name already exists.");

            existing.Name = entity.Name;
            existing.Value = entity.Value;
            existing.NumberOfPiecesInABox = entity.NumberOfPiecesInABox;
            existing.NumberOfPiecesInABundle = entity.NumberOfPiecesInABundle;
            existing.DenominationTypeId = entity.DenominationTypeId;
            existing.IsDeleted = entity.IsDeleted;
            existing.ModifiedDate = entity.ModifiedDate;
            existing.ModifiedBy = entity.ModifiedBy;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
