using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.CounterPartyTypes
{
    public class CounterPartyTypeRepository(ApplicationDbContext _db) : ICounterPartyTypeRepository
    {

        public async Task<ICollection<CounterPartyType>> FindAllAsync()
        {
            return await _db.CounterPartyTypes.ToListAsync();
        }

        public async Task<CounterPartyType> FindByIdAsync(int id)
        {
            return await _db.CounterPartyTypes.FindAsync(id);
        }

        public async Task<CounterPartyType> CreateAsync(CounterPartyType entity)
        {
            // Check for duplicate name
            var duplicate = await _db.CounterPartyTypes.AnyAsync(t => t.Name == entity.Name);
            if (duplicate)
                throw new InvalidOperationException("A counter party type with the same name already exists.");

            _db.CounterPartyTypes.Add(entity);
            await _db.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> UpdateAsync(CounterPartyType entity)
        {
            var existing = await _db.CounterPartyTypes.FindAsync(entity.Id);
            if (existing == null)
                return false;

            // Check for duplicate name (different id)
            var duplicate = await _db.CounterPartyTypes.AnyAsync(t => t.Name == entity.Name && t.Id != entity.Id);
            if (duplicate)
                throw new InvalidOperationException("A counter party type with the same name already exists.");

            existing.Name = entity.Name;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
