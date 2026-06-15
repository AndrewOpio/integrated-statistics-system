using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositProcessingFees
{
    public class DepositProcessingFeeRepository(VMSServiceDbContext _db) : IDepositProcessingFeeRepository
    {
        public async Task<bool> Create(DepositProcessingFee model)
        {
            _db.DepositProcessingFees.Add(model);
            return await Save();
        }

        public async Task<ICollection<DepositProcessingFee>> FindAll()
        {
            return await _db.DepositProcessingFees.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<DepositProcessingFee> FindById(int id)
        {
            return await _db.DepositProcessingFees
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DepositProcessingFees
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(DepositProcessingFee model)
        {
            _db.DepositProcessingFees.Update(model);
            return await Save();
        }
    }
}