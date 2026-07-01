using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageStatusHistories
{
    public class MonthlyShortageStatusHistoryRepository(VMSServiceDbContext _db)
        : IMonthlyShortageStatusHistoryRepository
    {
        public async Task<bool> Create(MonthlyShortageStatusHistory entity)
        {
            await _db.MonthlyShortageStatusHistories.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlyShortageStatusHistory>> FindAll()
        {
            return await _db.MonthlyShortageStatusHistories.AsNoTracking().ToListAsync();
        }

        public async Task<MonthlyShortageStatusHistory> FindById(int id)
        {
            return (await _db.MonthlyShortageStatusHistories.FindAsync(id))!;
        }

        public async Task<ICollection<MonthlyShortageStatusHistory>> FindByMonthlyShortageId(int monthlyShortageId)
        {
            return await _db.MonthlyShortageStatusHistories
                .AsNoTracking()
                .Where(h => h.MonthlyShortageId == monthlyShortageId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.MonthlyShortageStatusHistories.AnyAsync(h => h.Id == id);
        }

        public async Task<bool> Update(MonthlyShortageStatusHistory entity)
        {
            _db.MonthlyShortageStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
