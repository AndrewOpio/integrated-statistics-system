using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionTypes
{
    public class DestructionTypeRepository(VMSServiceDbContext _db) : IDestructionTypeRepository
    {
        public async Task<bool> Create(DestructionType entity)
        {
            _db.DestructionTypes.Add(entity);
            return await Save();
        }

        public async Task<ICollection<DestructionType>> FindAll()
        {
            return await _db.DestructionTypes
                .Where(x => x.IsDeleted == false)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<DestructionType> FindById(int id)
        {
            return await _db.DestructionTypes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DestructionTypes.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Update(DestructionType entity)
        {
            _db.DestructionTypes.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
