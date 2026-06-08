using Microsoft.EntityFrameworkCore;
using StockService.Data.Configurations;
using StockService.Data.Entities;
//using StockService.Core.Security;

namespace StockService.Core.Repository.StockLedgers
{
    public class StockLedgerRepository(ApplicationDbContext _db, IEncryptionService _encryptionService) : IStockLedgerRepository
    {
        public async Task<ICollection<StockLedger>> FindAllAsync()
        {
            var ledgers = await _db.StockLedger
                .AsNoTracking()
                .AsSplitQuery()
                .Include(sl => sl.CounterPartyType)
                .Include(sl => sl.StockLedgerDetails)
                    .ThenInclude(sld => sld.Category)
                .Include(sl => sl.StockLedgerDetails)
                    .ThenInclude(sld => sld.Denomination)
                .Include(sl => sl.StockLedgerDetails)
                    .ThenInclude(sld => sld.SubCategory)
                .ToListAsync();

            // Decrypt values after fetching
            foreach (var ledger in ledgers)
            {
                foreach (var detail in ledger.StockLedgerDetails)
                {
                    if (!string.IsNullOrEmpty(detail.Value))
                    {
                        detail.Value = _encryptionService.Decrypt(detail.Value).ToString();
                    }
                }
            }
            return ledgers;
        }

        public async Task<StockLedger> FindByIdAsync(int id)
        {
            var ledger = await _db.StockLedger
                .Include(cpt => cpt.CounterPartyType)
                .Include(sl => sl.StockLedgerDetails)
                    .ThenInclude(sld => sld.Denomination)
                .FirstOrDefaultAsync(sl => sl.Id == id);

            if (ledger != null)
            {
                foreach (var detail in ledger.StockLedgerDetails)
                {
                    if (!string.IsNullOrEmpty(detail.Value))
                    {
                        detail.Value = _encryptionService.Decrypt(detail.Value).ToString();
                    }
                }
            }
            return ledger;
        }

        public async Task<StockLedger> CreateAsync(StockLedger entity)
        {
            // Check for duplicate ReferenceCode
            var duplicate = await _db.StockLedger.AnyAsync(sl => sl.ReferenceCode == entity.ReferenceCode);
            // if (duplicate)
            //     throw new InvalidOperationException("A stock ledger with the same reference code already exists.");

            // Convert decimal values to encrypted strings before saving
            var encryptedDetails = new List<StockLedgerDetail>();
            foreach (var detail in entity.StockLedgerDetails ?? new List<StockLedgerDetail>())
            {
                encryptedDetails.Add(new StockLedgerDetail
                {
                    DenominationId = detail.DenominationId,
                    CategoryId = detail.CategoryId,
                    NumberOfPieces = detail.NumberOfPieces,
                    NumberOfBoxes = detail.NumberOfBoxes,
                    NumberOfBundles = detail.NumberOfBundles,
                    Value = _encryptionService.Encrypt(decimal.Parse(detail.Value))
                });
            }
            
            entity.StockLedgerDetails = [];

            _db.StockLedger.Add(entity);
            await _db.SaveChangesAsync();

            // Add encrypted details with the stock ledger ID
            foreach (var detail in encryptedDetails)
            {
                detail.StockLedgerId = entity.Id;
            }
            await _db.StockLedgerDetails.AddRangeAsync(encryptedDetails);
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
