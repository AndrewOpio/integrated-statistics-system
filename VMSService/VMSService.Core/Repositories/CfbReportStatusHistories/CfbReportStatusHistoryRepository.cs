using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbReportStatusHistories
{
    public class CfbReportStatusHistoryRepository(VMSServiceDbContext _db) : ICfbReportStatusHistoryRepository
    {
        public async Task<bool> Create(CfbReportStatusHistory entity)
        {
            _db.CfbReportStatusHistories.Add(entity);
            return await Save();
        }

        public async Task<ICollection<CfbReportStatusHistory>> FindAll()
        {
            return await _db.CfbReportStatusHistories
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<CfbReportStatusHistory> FindById(int id)
        {
            return await _db.CfbReportStatusHistories
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<CfbReportStatusHistory>> FindByCfbReportId(int cfbReportId)
        {
            return await _db.CfbReportStatusHistories
                .Where(x => x.CfbReportId == cfbReportId && !x.IsDeleted)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CfbReportStatusHistories.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Update(CfbReportStatusHistory entity)
        {
            _db.CfbReportStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
