using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.VaultSections
{
    public class VaultSectionRepository(VMSServiceDbContext _db) : IVaultSectionRepository
    {
        public async Task<bool> Create(VaultSection model)
        {
            _db.VaultSections.Add(model);
            return await Save();
        }

        public async Task<ICollection<VaultSection>> FindAll()
        {
            return await _db.VaultSections.Where(x => x.IsDeleted == false)
                .Include(x => x.Vault)
                .ToListAsync();
        }

        public async Task<VaultSection> FindById(int id)
        {
            return await _db.VaultSections
                .Include(x => x.Vault)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.VaultSections
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(VaultSection model)
        {
            _db.VaultSections.Update(model);
            return await Save();
        }
    }
}