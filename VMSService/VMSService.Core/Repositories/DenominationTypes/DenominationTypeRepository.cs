using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DenominationTypes
{
    public class DenominationTypeRepository(VMSServiceDbContext _db) : IDenominationTypeRepository
    {
        public async Task<bool> Create(DenominationType model)
        {
            _db.DenominationTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<DenominationType>> FindAll()
        {
            return await _db.DenominationTypes.ToListAsync();
        }

        public async Task<DenominationType> FindById(int id)
        {
            return await _db.DenominationTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DenominationTypes
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(DenominationType model)
        {
            _db.DenominationTypes.Update(model);
            return await Save();
        }
    }
}