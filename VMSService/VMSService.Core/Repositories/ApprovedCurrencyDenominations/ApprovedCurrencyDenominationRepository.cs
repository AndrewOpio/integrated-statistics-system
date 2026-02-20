using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ApprovedCurrencyDenominations
{
    public class ApprovedCurrencyDenominationRepository(VMSServiceDbContext _db) : IApprovedCurrencyDenominationRepository
    {
        public async Task<bool> Create(ApprovedCurrencyDenomination model)
        {
            _db.ApprovedCurrencyDenominations.Add(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyDenomination>> FindAll()
        {
            return await _db.ApprovedCurrencyDenominations.ToListAsync();
        }

        public async Task<ApprovedCurrencyDenomination> FindById(int id)
        {
            return await _db.ApprovedCurrencyDenominations
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ApprovedCurrencyDenominations
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ApprovedCurrencyDenomination model)
        {
            _db.ApprovedCurrencyDenominations.Update(model);
            return await Save();
        }
    }
}