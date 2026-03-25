using ConfigurationsService.Data.Configurations;
using ConfigurationsService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationsService.Core.Repositories.Districts
{
    public class DistrictRepository(ApplicationDbContext _db) : IDistrictRepository
    {
        public async Task<District> Create(District model)
        {
            // Check for duplicate district name
            var duplicate = await _db.Districts.AnyAsync(d => d.Name == model.Name && !d.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A district with the same name already exists.");

            _db.Districts.Add(model);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ICollection<District>> FindAll()
        {
            return await _db.Districts.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<District> FindById(int id)
        {
            return await _db.Districts.Where(d => !d.IsDeleted)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Districts.Where(d => !d.IsDeleted)
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(District model)
        {
            // Check for duplicate district name (different id)
            var duplicate = await _db.Districts.AnyAsync(d => d.Name == model.Name && d.Id != model.Id && !d.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A district with the same name already exists.");

            var existing = await _db.Districts.FindAsync(model.Id);
            if (existing == null)
                return false;

            existing.Name = model.Name;
            existing.IsDeleted = model.IsDeleted;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = model.ModifiedDate;

            return await Save();
        }

        public async Task<District> FindByName(string districtName)
        {
            return await _db.Districts
                .Where(x => x.Name == districtName && !x.IsDeleted).FirstOrDefaultAsync();
        }
    }
}