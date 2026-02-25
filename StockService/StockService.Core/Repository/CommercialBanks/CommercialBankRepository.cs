using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.CommercialBanks
{
    public class CommercialBankRepository(ApplicationDbContext _db) : ICommercialBankRepository
    {

        public async Task<ICollection<CommercialBank>> FindAllAsync()
        {
            return await _db.CommercialBanks.ToListAsync();
        }

        public async Task<CommercialBank> FindByIdAsync(int id)
        {
            return await _db.CommercialBanks.FindAsync(id);
        }

        public async Task<CommercialBank> CreateAsync(CommercialBank bank)
        {
            // Check for duplicate bank name or code
            var duplicate = await _db.CommercialBanks.AnyAsync(b => b.Name == bank.Name || b.Code == bank.Code);
            if (duplicate)
                throw new InvalidOperationException("A commercial bank with the same name or code already exists.");

            _db.CommercialBanks.Add(bank);
            await _db.SaveChangesAsync();
            return bank;
        }

        public async Task<bool> UpdateAsync(CommercialBank bank)
        {
            var existing = await _db.CommercialBanks.FindAsync(bank.Id);
            if (existing == null)
                return false;

            // Check for duplicate bank name or code (different id)
            var duplicate = await _db.CommercialBanks.AnyAsync(b => (b.Name == bank.Name || b.Code == bank.Code) && b.Id != bank.Id);
            if (duplicate)
                throw new InvalidOperationException("A commercial bank with the same name or code already exists.");

            existing.Name = bank.Name;
            existing.Code = bank.Code;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
