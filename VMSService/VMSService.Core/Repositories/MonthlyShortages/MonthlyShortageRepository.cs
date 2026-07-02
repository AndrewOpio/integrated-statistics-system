using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortages
{
    public class MonthlyShortageRepository(VMSServiceDbContext _db) : IMonthlyShortageRepository
    {
        public async Task<bool> Create(MonthlyShortage entity)
        {
            await _db.MonthlyShortages.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlyShortage>> FindAll()
            => await _db.MonthlyShortages
                .AsNoTracking()
                .OrderByDescending(s => s.Period)
                .ToListAsync();

        public async Task<MonthlyShortage> FindById(int id)
            => (await _db.MonthlyShortages.FindAsync(id))!;

        public async Task<MonthlyShortage?> FindByPeriod(DateTime period)
        {
            var start = new DateTime(period.Year, period.Month, 1);
            var end   = start.AddMonths(1);
            return await _db.MonthlyShortages
                .FirstOrDefaultAsync(s => s.Period >= start && s.Period < end);
        }

        public async Task<bool> IsExists(int id)
            => await _db.MonthlyShortages.AnyAsync(s => s.Id == id);

        public async Task<bool> Update(MonthlyShortage entity)
        {
            _db.MonthlyShortages.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
