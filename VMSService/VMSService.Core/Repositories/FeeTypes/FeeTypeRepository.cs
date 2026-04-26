using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.FeeTypes
{
    public class FeeTypeRepository(VMSServiceDbContext _db) : IFeeTypeRepository
    {
        public async Task<bool> Create(FeeType model)
        {
            _db.FeeTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<FeeType>> FindAll()
        {
            return await _db.FeeTypes
                .ToListAsync();
        }

        public async Task<FeeType> FindById(int id)
        {
            return await _db.FeeTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<FeeType?> FindByName(string name)
        {
            return await _db.FeeTypes
                .FirstOrDefaultAsync(x => x.Name == name);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.FeeTypes
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(FeeType model)
        {
            _db.FeeTypes.Update(model);
            return await Save();
        }
    }
}