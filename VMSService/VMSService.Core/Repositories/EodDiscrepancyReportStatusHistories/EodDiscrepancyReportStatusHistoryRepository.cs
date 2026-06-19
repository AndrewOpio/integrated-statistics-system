using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories
{
    public class EodDiscrepancyReportStatusHistoryRepository(VMSServiceDbContext _db) : IEodDiscrepancyReportStatusHistoryRepository
    {
        public async Task<bool> Create(EodDiscrepancyReportStatusHistory entity)
        {
            _db.EodDiscrepancyReportStatusHistories.Add(entity);
            return await Save();
        }

        public async Task<ICollection<EodDiscrepancyReportStatusHistory>> FindAll()
        {
            return await _db.EodDiscrepancyReportStatusHistories
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<EodDiscrepancyReportStatusHistory> FindById(int id)
        {
            return await _db.EodDiscrepancyReportStatusHistories
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<EodDiscrepancyReportStatusHistory>> FindByCfbReportId(int cfbReportId)
        {
            return await _db.EodDiscrepancyReportStatusHistories
                .Where(x => x.EodDiscrepancyReportId == cfbReportId && !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.EodDiscrepancyReportStatusHistories.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Update(EodDiscrepancyReportStatusHistory entity)
        {
            _db.EodDiscrepancyReportStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
