using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionLocations
{
    public class DestructionLocationRepository(VMSServiceDbContext _db) : IDestructionLocationRepository
    {
        public async Task<bool> Create(DestructionLocation entity)
        {
            _db.DestructionLocations.Add(entity);
            return await Save();
        }

        public async Task<ICollection<DestructionLocation>> FindAll()
        {
            return await _db.DestructionLocations
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<DestructionLocation> FindById(int id)
        {
            return await _db.DestructionLocations.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionLocations.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Update(DestructionLocation entity)
        {
            _db.DestructionLocations.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
