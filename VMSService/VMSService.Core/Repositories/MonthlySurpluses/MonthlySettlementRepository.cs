using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlySurpluses
{
    public class MonthlySurplusRepository(VMSServiceDbContext _db,
        ILogger<MonthlySurplusRepository> _logger) : IMonthlySurplusRepository
    {
        public async Task<bool> Create(MonthlySurplus entity)
        {
            await _db.MonthlySurpluses.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlySurplus>> FindAll()
        {
            return await _db.MonthlySurpluses
                .AsNoTracking()
                .OrderByDescending(r => r.Period)
                .ToListAsync();
        }

        public async Task<MonthlySurplus> FindById(int id)
        {
            return (await _db.MonthlySurpluses
                .Include(r => r.StatusHistories)
                .FirstOrDefaultAsync(r => r.Id == id))!;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.MonthlySurpluses.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(MonthlySurplus entity)
        {
            _db.MonthlySurpluses.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
