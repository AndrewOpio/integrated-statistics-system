using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.LegacyBags
{
    public class LegacyBagRepository(VMSServiceDbContext _db) : ILegacyBagRepository
    {
        public async Task<bool> Create(LegacyBag model)
        {
            _db.LegacyBags.Add(model);
            return await Save();
        }

        public async Task<ICollection<LegacyBag>> FindAll()
        {
            return await _db.LegacyBags.ToListAsync();
        }

        public async Task<LegacyBag> FindById(int id)
        {
            return await _db.LegacyBags
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.LegacyBags
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(LegacyBag model)
        {
            _db.LegacyBags.Update(model);
            return await Save();
        }
    }
}
