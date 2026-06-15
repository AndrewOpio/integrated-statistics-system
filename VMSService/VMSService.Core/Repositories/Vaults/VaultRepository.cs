using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.Vaults
{
    public class VaultRepository(VMSServiceDbContext _db) : IVaultRepository
    {
        public async Task<bool> Create(Vault model)
        {
            _db.Vaults.Add(model);
            return await Save();
        }

        public async Task<ICollection<Vault>> FindAll()
        {
            return await _db.Vaults.Where(x => x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Vault> FindById(int id)
        {
            return await _db.Vaults
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Vaults
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Vault model)
        {
            _db.Vaults.Update(model);
            return await Save();
        }
    }
}