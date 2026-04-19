using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PackingListRanges
{
    public class PackingListRangeRepository(VMSServiceDbContext _db) : IPackingListRangeRepository
    {
        public async Task<ICollection<PackingListRange>> FindAllAsync()
        {
            return await _db.PackingListRanges
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<PackingListRange> FindByIdAsync(int id)
        {
            return await _db.PackingListRanges
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ICollection<PackingListRange>> FindByExStockIdAsync(int exStockId)
        {
            return await _db.PackingListRanges
                .AsNoTracking()
                .Where(p => p.ExpectedStockId == exStockId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<ICollection<PackingListRange>> FindAllActiveAsync()
        {
            return await _db.PackingListRanges
                .AsNoTracking()
                .Include(x => x.ExpectedStockDetail)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<PackingListRange> CreateAsync(PackingListRange entity)
        {
            await _db.PackingListRanges.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(PackingListRange entity)
        {
            _db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await FindByIdAsync(id);
            if (entity == null)
                return false;

            _db.PackingListRanges.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
