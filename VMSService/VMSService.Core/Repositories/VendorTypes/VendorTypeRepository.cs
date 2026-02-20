using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.VendorTypes
{
    public class VendorTypeRepository(VMSServiceDbContext _db) : IVendorTypeRepository
    {
        public async Task<bool> Create(VendorType model)
        {
            _db.VendorTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<VendorType>> FindAll()
        {
            return await _db.VendorTypes.ToListAsync();
        }

        public async Task<VendorType> FindById(int id)
        {
            return await _db.VendorTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.VendorTypes
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(VendorType model)
        {
            _db.VendorTypes.Update(model);
            return await Save();
        }
    }
}