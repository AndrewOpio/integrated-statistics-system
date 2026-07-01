using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyCommercialBankSettlements
{
    public class MonthlyCommercialBankSettlementRepository(VMSServiceDbContext _db)
        : IMonthlyCommercialBankSettlementRepository
    {
        public async Task<bool> Create(MonthlyCommercialBankSettlement entity)
        {
            await _db.MonthlyCommercialBankSettlements.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlyCommercialBankSettlement>> FindAll()
            => await _db.MonthlyCommercialBankSettlements
                .AsNoTracking()
                .OrderByDescending(s => s.Period)
                .ThenBy(s => s.CommercialBankId)
                .ToListAsync();

        public async Task<MonthlyCommercialBankSettlement?> FindById(int id)
            => await _db.MonthlyCommercialBankSettlements
                .Include(s => s.StatusHistories)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<MonthlyCommercialBankSettlement?> FindByPeriodAndBank(DateTime period, int commercialBankId)
        {
            var start = new DateTime(period.Year, period.Month, 1);
            var end   = start.AddMonths(1);
            return await _db.MonthlyCommercialBankSettlements
                .FirstOrDefaultAsync(s => s.Period >= start && s.Period < end && s.CommercialBankId == commercialBankId);
        }

        public async Task<bool> IsExists(int id)
            => await _db.MonthlyCommercialBankSettlements.AnyAsync(s => s.Id == id);

        public async Task<bool> Update(MonthlyCommercialBankSettlement entity)
        {
            _db.MonthlyCommercialBankSettlements.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
