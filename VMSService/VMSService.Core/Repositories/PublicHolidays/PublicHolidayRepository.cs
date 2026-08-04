using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PublicHolidays
{
    public class PublicHolidayRepository(VMSServiceDbContext _db) : IPublicHolidayRepository
    {
        public async Task<ICollection<PublicHoliday>> FindAll()
            => await _db.PublicHolidays.OrderBy(h => h.Date).ToListAsync();

        public async Task<ICollection<PublicHoliday>> FindByActive(bool active)
            => await _db.PublicHolidays.Where(h => h.Active == active).OrderBy(h => h.Date).ToListAsync();

        public async Task<PublicHoliday> FindById(int id)
            => await _db.PublicHolidays.FirstOrDefaultAsync(h => h.Id == id);

        public async Task<PublicHoliday?> FindByDateAsync(DateOnly date)
            => await _db.PublicHolidays.FirstOrDefaultAsync(h => h.Active && h.Date == date);

        public async Task<bool> IsExists(int id)
            => await _db.PublicHolidays.AnyAsync(h => h.Id == id);

        public async Task<bool> Create(PublicHoliday entity)
        {
            await _db.PublicHolidays.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(PublicHoliday entity)
        {
            _db.PublicHolidays.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> Save() => await _db.SaveChangesAsync() > 0;
    }
}
