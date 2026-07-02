using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlySurplusStatusHistories
{
    public class MonthlySurplusStatusHistoryRepository(VMSServiceDbContext _db)
        : IMonthlySurplusStatusHistoryRepository
    {
        public async Task<bool> Create(MonthlySurplusStatusHistory entity)
        {
            await _db.MonthlySurplusStatusHistories.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlySurplusStatusHistory>> FindAll()
        {
            return await _db.MonthlySurplusStatusHistories.AsNoTracking().ToListAsync();
        }

        public async Task<MonthlySurplusStatusHistory> FindById(int id)
        {
            return (await _db.MonthlySurplusStatusHistories.FindAsync(id))!;
        }

        public async Task<ICollection<MonthlySurplusStatusHistory>> FindByMonthlySurplusId(int monthlySurplusId)
        {
            return await _db.MonthlySurplusStatusHistories
                .AsNoTracking()
                .Where(h => h.MonthlySurplusId == monthlySurplusId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.MonthlySurplusStatusHistories.AnyAsync(h => h.Id == id);
        }

        public async Task<bool> Update(MonthlySurplusStatusHistory entity)
        {
            _db.MonthlySurplusStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
