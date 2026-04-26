using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositFees
{
    public class DepositFeeRepository(VMSServiceDbContext _db) : IDepositFeeRepository
    {
        public async Task<bool> Create(DepositFee model)
        {
            _db.DepositFees.Add(model);
            return await Save();
        }

        public async Task<ICollection<DepositFee>> FindAll()
        {
            return await _db.DepositFees
                .Where(x => x.IsDeleted == false)
                .Include(x => x.FeeType)
                .Include(x => x.DepositRequest)
                .ToListAsync();
        }

        public async Task<DepositFee> FindById(int id)
        {
            return await _db.DepositFees
                .Include(x => x.FeeType)
                .Include(x => x.DepositRequest)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<DepositFee>> GetByDepositRequestId(Guid depositRequestId)
        {
            return await _db.DepositFees
                .Where(x => x.DepositRequestId == depositRequestId && x.IsDeleted == false)
                .Include(x => x.FeeType)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.DepositFees
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<bool> Update(DepositFee model)
        {
            _db.DepositFees.Update(model);
            return await Save();
        }
    }
}