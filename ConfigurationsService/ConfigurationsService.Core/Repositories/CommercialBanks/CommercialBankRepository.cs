using ConfigurationsService.Data.Configurations;
using ConfigurationsService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationsService.Core.Repositories.CommercialBanks
{
    public class CommercialBankRepository(ApplicationDbContext _db) : ICommercialBankRepository
    {
        public async Task<CommercialBank> Create(CommercialBank model)
        {
            // Check for duplicate bank name
            var duplicate = await _db.CommercialBanks.AnyAsync(b => b.Name == model.Name && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A commercial bank with the same name already exists.");

            // Check for duplicate bank code
            var duplicateCode = await _db.CommercialBanks.AnyAsync(b => b.Code == model.Code && !b.IsDeleted);
            if (duplicateCode)
                throw new InvalidOperationException("A commercial bank with the same code already exists.");

            _db.CommercialBanks.Add(model);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ICollection<CommercialBank>> FindAll()
        {
            return await _db.CommercialBanks.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<CommercialBank> FindById(int id)
        {
            return await _db.CommercialBanks.Where(b => !b.IsDeleted)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CommercialBanks.Where(b => !b.IsDeleted)
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CommercialBank model)
        {
            // Check for duplicate bank name (different id)
            var duplicate = await _db.CommercialBanks.AnyAsync(b => b.Name == model.Name && b.Id != model.Id && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A commercial bank with the same name already exists.");

            // Check for duplicate bank code (different id)
            var duplicateCode = await _db.CommercialBanks.AnyAsync(b => b.Code == model.Code && b.Id != model.Id && !b.IsDeleted);
            if (duplicateCode)
                throw new InvalidOperationException("A commercial bank with the same code already exists.");

            var existing = await _db.CommercialBanks.FindAsync(model.Id);
            if (existing == null)
                return false;

            existing.Name = model.Name;
            existing.CBCode = model.CBCode;
            existing.Code = model.Code;
            existing.IsDeleted = model.IsDeleted;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = model.ModifiedDate;

            return await Save();
        }

        public async Task<CommercialBank> FindByName(string bankName)
        {
            return await _db.CommercialBanks
                .Where(x => x.Name == bankName && !x.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<CommercialBank> FindByCode(string code)
        {
            return await _db.CommercialBanks
                .Where(x => x.Code == code && !x.IsDeleted).FirstOrDefaultAsync();
        }
    }
}