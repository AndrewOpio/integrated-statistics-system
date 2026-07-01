using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories
{
    public class MonthlyCommercialBankSettlementStatusHistoryRepository(VMSServiceDbContext _db)
        : IMonthlyCommercialBankSettlementStatusHistoryRepository
    {
        public async Task<bool> Create(MonthlyCommercialBankSettlementStatusHistory entity)
        {
            await _db.MonthlyCommercialBankSettlementStatusHistories.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<MonthlyCommercialBankSettlementStatusHistory>> FindBySettlementId(int settlementId)
            => await _db.MonthlyCommercialBankSettlementStatusHistories
                .AsNoTracking()
                .Where(h => h.MonthlyCommercialBankSettlementId == settlementId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
