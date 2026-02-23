using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Denominations
{
    public class DenominationRepository(VMSServiceDbContext _db) : IDenominationRepository
    {
        public async Task<bool> Create(Denomination model)
        {
            _db.Denominations.Add(model);
            return await Save();
        }

        public async Task<ICollection<Denomination>> FindAll()
        {
            return await _db.Denominations.Where(x => x.IsDeleted == false)
                .Include(x => x.DenominationType)
                .ToListAsync();
        }

        public async Task<Denomination> FindById(int id)
        {
            return await _db.Denominations
                .Include(x => x.DenominationType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Denominations
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Denomination model)
        {
            _db.Denominations.Update(model);
            return await Save();
        }
    }
}