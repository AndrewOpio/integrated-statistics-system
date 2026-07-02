using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingShortageStatusHistories
{
    public class BankingShortageStatusHistoryRepository(VMSServiceDbContext _db)
        : IBankingShortageStatusHistoryRepository
    {
        public async Task<bool> Create(BankingShortageStatusHistory entity)
        {
            await _db.BankingShortageStatusHistories.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BankingShortageStatusHistory>> FindAll()
            => await _db.BankingShortageStatusHistories.AsNoTracking().ToListAsync();

        public async Task<BankingShortageStatusHistory> FindById(int id)
            => (await _db.BankingShortageStatusHistories.FindAsync(id))!;

        public async Task<ICollection<BankingShortageStatusHistory>> FindByBankingShortageId(int bankingShortageId)
            => await _db.BankingShortageStatusHistories
                .AsNoTracking()
                .Where(h => h.BankingShortageId == bankingShortageId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

        public async Task<bool> IsExists(int id)
            => await _db.BankingShortageStatusHistories.AnyAsync(h => h.Id == id);

        public async Task<bool> Update(BankingShortageStatusHistory entity)
        {
            _db.BankingShortageStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
