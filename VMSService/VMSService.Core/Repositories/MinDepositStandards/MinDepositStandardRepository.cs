using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MinDepositStandards
{
    public class MinDepositStandardRepository(VMSServiceDbContext _db) : IMinDepositStandardRepository
    {
        public async Task<bool> Create(MinDepositStandard model)
        {
            _db.MinDepositStandards.Add(model);
            return await Save();
        }

        public async Task<ICollection<MinDepositStandard>> FindAll()
        {
            return await _db.MinDepositStandards.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<MinDepositStandard> FindById(int id)
        {
            return await _db.MinDepositStandards
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.MinDepositStandards
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(MinDepositStandard model)
        {
            _db.MinDepositStandards.Update(model);
            return await Save();
        }
    }
}