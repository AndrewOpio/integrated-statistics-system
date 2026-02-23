using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExpectedCurrencies
{
    public class ExPrintMintRepository(VMSServiceDbContext _db) : IExPrintMintRepository
    {
        public async Task<bool> Create(ExPrintMint model)
        {
            _db.ExPrintMints.Add(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMint>> FindAll()
        {
            return await _db.ExPrintMints
                .Where(x => x.IsDeleted == false)
                .Include(x => x.CurrentStatus)
                .Include(x => x.CurrencyVendor)
                .Include(x => x.CurrencyVendor.VendorType)
                .Include(x => x.ApprovedCurrencyProcurement)
                .Include(x => x.Denomination)
                .ToListAsync();
        }

        public async Task<ExPrintMint> FindById(int id)
        {
            return await _db.ExPrintMints
                .Include(x => x.CurrentStatus)
                .Include(x => x.CurrencyVendor)
                .Include(x => x.CurrencyVendor.VendorType)
                .Include(x => x.ApprovedCurrencyProcurement)
                .Include(x => x.Denomination)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ExPrintMints
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ExPrintMint model)
        {
            _db.ExPrintMints.Update(model);
            return await Save();
        }
    }
}