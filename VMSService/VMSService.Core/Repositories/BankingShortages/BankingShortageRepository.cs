using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingShortages
{
    public class BankingShortageRepository(VMSServiceDbContext _db) : IBankingShortageRepository
    {
        public async Task<bool> Create(BankingShortage entity)
        {
            await _db.BankingShortages.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BankingShortage>> FindAll()
            => await _db.BankingShortages
                .AsNoTracking()
                .OrderByDescending(s => s.Period)
                .ToListAsync();

        public async Task<BankingShortage> FindById(int id)
            => (await _db.BankingShortages.FindAsync(id))!;

        public async Task<BankingShortage?> FindByPeriod(DateTime period)
        {
            var start = new DateTime(period.Year, period.Month, 1);
            var end   = start.AddMonths(1);
            return await _db.BankingShortages
                .FirstOrDefaultAsync(s => s.Period >= start && s.Period < end);
        }

        public async Task<bool> IsExists(int id)
            => await _db.BankingShortages.AnyAsync(s => s.Id == id);

        public async Task<bool> Update(BankingShortage entity)
        {
            _db.BankingShortages.Update(entity);
            return await Save();
        }

        public async Task<int> CountForPeriod(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end   = start.AddMonths(1);
            return await _db.BankingShortages
                .CountAsync(s => s.Period >= start && s.Period < end);
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
