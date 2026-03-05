using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;

namespace StockService.Core.Repository.StockLedgers
{
    public class StockLedgerRepository(ApplicationDbContext _db) : IStockLedgerRepository
    {
        public async Task<ICollection<StockLedger>> FindAllAsync()
        {
            return await _db.StockLedger
                .Include(cpt => cpt.CounterPartyType)
                .Include(st => st.Store)
                .Include(sl => sl.StockLedgerDetails)
                   .ThenInclude(sld => sld.Denomination)
                .ToListAsync();
        }

        public async Task<StockLedger> FindByIdAsync(int id)
        {
            return await _db.StockLedger
                .Include(cpt => cpt.CounterPartyType)
                .Include(st => st.Store)
                .Include(sl => sl.StockLedgerDetails)
                    .ThenInclude(sld => sld.Denomination)
                .FirstOrDefaultAsync(sl => sl.Id == id);
        }

        public async Task<StockLedger> CreateAsync(StockLedger entity)
        {
            // Check for duplicate ReferenceCode
            var duplicate = await _db.StockLedger.AnyAsync(sl => sl.ReferenceCode == entity.ReferenceCode);
            if (duplicate)
                throw new InvalidOperationException("A stock ledger with the same reference code already exists.");

            ICollection<StockLedgerDetail> stockLedgerDetails = entity.StockLedgerDetails;
            entity.StockLedgerDetails = [];

            _db.StockLedger.Add(entity);
            await _db.SaveChangesAsync();

            // Create StockLedgerDetails
            foreach (StockLedgerDetail stockLedgerDetail in stockLedgerDetails)
            {
                stockLedgerDetail.StockLedgerId = entity.Id;
            }
            await _db.StockLedgerDetails.AddRangeAsync(stockLedgerDetails);
            await _db.SaveChangesAsync();

            return entity;
        }

        public Task<bool> UpdateAsync(StockLedger entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
