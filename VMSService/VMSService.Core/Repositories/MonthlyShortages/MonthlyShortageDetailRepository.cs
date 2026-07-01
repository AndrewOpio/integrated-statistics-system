using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortages
{
    public class MonthlyShortageDetailRepository(VMSServiceDbContext _db) : IMonthlyShortageDetailRepository
    {
        public async Task<ICollection<MonthlyShortageDetail>> FindByMonthlyShortageId(int monthlyShortageId)
            => await _db.MonthlyShortageDetails
                .AsNoTracking()
                .Where(d => d.MonthlyShortageId == monthlyShortageId)
                .OrderBy(d => d.BranchId)
                .ToListAsync();

        public async Task<MonthlyShortageDetail?> FindByMonthlyShortageIdAndBranch(int monthlyShortageId, int branchId)
            => await _db.MonthlyShortageDetails
                .FirstOrDefaultAsync(d => d.MonthlyShortageId == monthlyShortageId && d.BranchId == branchId);

        public async Task<bool> Create(MonthlyShortageDetail entity)
        {
            await _db.MonthlyShortageDetails.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(MonthlyShortageDetail entity)
        {
            _db.MonthlyShortageDetails.Update(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteByMonthlyShortageId(int monthlyShortageId)
        {
            var details = await _db.MonthlyShortageDetails
                .Where(d => d.MonthlyShortageId == monthlyShortageId)
                .ToListAsync();
            _db.MonthlyShortageDetails.RemoveRange(details);
            return await _db.SaveChangesAsync() >= 0;
        }
    }
}
