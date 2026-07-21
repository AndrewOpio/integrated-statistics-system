using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageReimbursements
{
    public class MonthlyShortageReimbursementStatusHistoryRepository(VMSServiceDbContext _db)
        : IMonthlyShortageReimbursementStatusHistoryRepository
    {
        public async Task<List<MonthlyShortageReimbursementStatusHistory>> FindByReimbursementId(int reimbursementId)
            => await _db.MonthlyShortageReimbursementStatusHistories
                .AsNoTracking()
                .Where(h => h.MonthlyShortageReimbursementId == reimbursementId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

        public async Task<MonthlyShortageReimbursementStatusHistory> Create(MonthlyShortageReimbursementStatusHistory entity)
        {
            await _db.MonthlyShortageReimbursementStatusHistories.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
    }
}
