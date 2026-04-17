using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PackingListRangeDetails
{
    public class PackingListDetailsRepository(VMSServiceDbContext _db) : IPackingListDetailsRepository
    {
        public async Task<ICollection<PackingListDetails>> FindByPackingListRangeIdAsync(int packingListRangeId)
        {
            return await _db.PackingListDetails
                .AsNoTracking()
                .Where(p => p.PackingListRangeId == packingListRangeId)
                .OrderBy(p => p.CaseNumber)
                .ThenBy(p => p.SerialNoteStartNumber)
                .ToListAsync();
        }

        public async Task<PackingListDetails> FindByIdAsync(long id)
        {
            return await _db.PackingListDetails
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PackingListDetails> CreateAsync(PackingListDetails entity)
        {
            await _db.PackingListDetails.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> CreateBatchAsync(List<PackingListDetails> entities)
        {
            if (entities == null || entities.Count == 0)
                return false;

            await _db.PackingListDetails.AddRangeAsync(entities);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByRangeIdAsync(int rangeId)
        {
            var details = await _db.PackingListDetails
                .Where(p => p.PackingListRangeId == rangeId)
                .ToListAsync();

            if (details.Count == 0)
                return false;

            _db.PackingListDetails.RemoveRange(details);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
