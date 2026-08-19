using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingSurpluses
{
    public class BankingSurplusRepository(VMSServiceDbContext _db,
        ILogger<BankingSurplusRepository> _logger) : IBankingSurplusRepository
    {
        public async Task<bool> Create(BankingSurplus entity)
        {
            await _db.BankingSurpluses.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BankingSurplus>> FindAll()
        {
            return await _db.BankingSurpluses
                .AsNoTracking()
                .OrderByDescending(r => r.Period)
                .ToListAsync();
        }

        public async Task<BankingSurplus> FindById(int id)
        {
            return (await _db.BankingSurpluses
                .Include(r => r.StatusHistories)
                .FirstOrDefaultAsync(r => r.Id == id))!;
        }

        public async Task<BankingSurplus?> FindByBranchAndPeriod(int branchId, DateTime periodStart)
        {
            return await _db.BankingSurpluses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.BranchId == branchId && r.Period == periodStart);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BankingSurpluses.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(BankingSurplus entity)
        {
            _db.BankingSurpluses.Update(entity);
            return await Save();
        }

        public async Task<int> CountForPeriodAndBranch(int branchId, int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end   = start.AddMonths(1);
            return await _db.BankingSurpluses
                .CountAsync(s => s.BranchId == branchId && s.Period >= start && s.Period < end);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
