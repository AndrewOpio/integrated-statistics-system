using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ReceivedStocks
{
    public class ReceivedStockRepository(VMSServiceDbContext _db) : IReceivedStockRepository
    {
        public async Task<bool> Create(ReceivedStock model)
        {
            _db.ReceivedStock.Add(model);
            return await Save();
        }

        public async Task<ICollection<ReceivedStock>> FindAll()
        {
            return await _db.ReceivedStock
                .OrderByDescending(x => x.CreatedDate)
                .Where(x => x.IsDeleted == false)
                .Include(x => x.ExPrintMint)
                .Include(x => x.ExPrintMint.CurrencyVendor)
                .Include(x => x.ExPrintMint.CurrencyVendor.VendorType)
                .ToListAsync();
        }

        public async Task<ReceivedStock> FindById(int id)
        {
            return await _db.ReceivedStock
                .Include(x => x.ExPrintMint)
                .Include(x => x.ExPrintMint.CurrencyVendor)
                .Include(x => x.ExPrintMint.CurrencyVendor.VendorType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ReceivedStock
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ReceivedStock model)
        {
            _db.ReceivedStock.Update(model);
            return await Save();
        }

        public async Task<int> CreateNew(ReceivedStock model)
        {
            _db.ReceivedStock.Add(model);
            await Save();
            return model.Id;
        }
    }
}