using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageReimbursements
{
    public class MonthlyShortageReimbursementRepository(VMSServiceDbContext _db) : IMonthlyShortageReimbursementRepository
    {
        public async Task<List<MonthlyShortageReimbursement>> FindAll()
            => await _db.MonthlyShortageReimbursements
                .AsNoTracking()
                .Include(r => r.Denominations)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

        public async Task<MonthlyShortageReimbursement?> FindById(int id)
            => await _db.MonthlyShortageReimbursements
                .Include(r => r.Denominations)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<bool> ExistsForShortageAndBranch(int monthlyShortageId, int branchId)
            => await _db.MonthlyShortageReimbursements
                .AnyAsync(r => r.MonthlyShortageId == monthlyShortageId && r.BranchId == branchId);

        public async Task<MonthlyShortageReimbursement> Create(MonthlyShortageReimbursement entity)
        {
            await _db.MonthlyShortageReimbursements.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task Update(MonthlyShortageReimbursement entity)
        {
            _db.MonthlyShortageReimbursements.Update(entity);
            await _db.SaveChangesAsync();
        }
    }
}
