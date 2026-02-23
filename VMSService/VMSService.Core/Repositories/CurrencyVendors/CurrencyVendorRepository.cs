using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CurrencyVendors
{
    public class CurrencyVendorRepository(VMSServiceDbContext _db) : ICurrencyVendorRepository
    {
        public async Task<bool> Create(CurrencyVendor model)
        {
            _db.CurrencyVendors.Add(model);
            return await Save();
        }

        public async Task<ICollection<CurrencyVendor>> FindAll()
        {
            return await _db.CurrencyVendors.Where(x => x.IsDeleted == false)
                .Include(x => x.VendorType)
                .ToListAsync();
        }

        public async Task<CurrencyVendor> FindById(int id)
        {
            return await _db.CurrencyVendors
                .Include(x => x.VendorType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CurrencyVendors
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CurrencyVendor model)
        {
            _db.CurrencyVendors.Update(model);
            return await Save();
        }
    }
}