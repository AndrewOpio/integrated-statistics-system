using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ApprovedCurrencyProcurements
{
    public class ApprovedCurrencyProcurementRepository(VMSServiceDbContext _db) : IApprovedCurrencyProcurementRepository
    {
        public async Task<bool> Create(ApprovedCurrencyProcurement model)
        {
            _db.ApprovedCurrencyProcurements.Add(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyProcurement>> FindAll()
        {
            return await _db.ApprovedCurrencyProcurements.Where(x => x.IsDeleted == false)
                .Include(x => x.CurrentStatus)
                .ToListAsync();
        }

        public async Task<ApprovedCurrencyProcurement> FindById(int id)
        {
            return await _db.ApprovedCurrencyProcurements
                .Include(x => x.CurrentStatus)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ApprovedCurrencyProcurements
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ApprovedCurrencyProcurement model)
        {
            _db.ApprovedCurrencyProcurements.Update(model);
            return await Save();
        }
    }
}
