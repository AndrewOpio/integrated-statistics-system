using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ApprovedCurrencyStatusHistories
{
    public class ApprovedCurrencyStatusHistoryRepository(VMSServiceDbContext _db) : IApprovedCurrencyStatusHistoryRepository
    {
        public async Task<bool> Create(ApprovedCurrencyStatusHistory model)
        {
            _db.ApprovedCurrencyStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyStatusHistory>> FindAll()
        {
            return await _db.ApprovedCurrencyStatusHistory.Where(x => x.IsDeleted == false)
                .Include(x => x.ApprovedCurrencyProcurement)
                .ToListAsync();
        }

        public async Task<ApprovedCurrencyStatusHistory> FindById(int id)
        {
            return await _db.ApprovedCurrencyStatusHistory
                .Include(x => x.ApprovedCurrencyProcurement)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ApprovedCurrencyStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ApprovedCurrencyStatusHistory model)
        {
            _db.ApprovedCurrencyStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyStatusHistory>> FindByApprovedCurrencyId(int approvedCurrencyProcurementId)
        {
            return await _db.ApprovedCurrencyStatusHistory
                .Where(x => x.ApprovedCurrencyProcurementId == approvedCurrencyProcurementId)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }
    }
}