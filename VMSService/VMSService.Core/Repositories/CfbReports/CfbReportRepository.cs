using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbReports
{
    public class CfbReportRepository(VMSServiceDbContext _db,
        ILogger<CfbReportRepository> _logger) : ICfbReportRepository
    {
        public async Task<bool> Create(CfbReport entity)
        {
            await _db.CfbReports.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<CfbReport>> FindAll()
        {
            return await _db.CfbReports
                .AsNoTracking()
                .Include(r => r.CfbReportTransactions)
                .OrderByDescending(r => r.PeriodStart)
                .ToListAsync();
        }

        public async Task<CfbReport> FindById(int id)
        {
            var result = await _db.CfbReports
                .Include(r => r.CfbReportTransactions)
                .Include(r => r.CfbReportStatusHistories)
                .FirstOrDefaultAsync(r => r.Id == id);
            return result!;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CfbReports.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(CfbReport entity)
        {
            _db.CfbReports.Update(entity);
            return await Save();
        }

        public async Task<bool> UpdateWithChildren(CfbReport entity)
        {
            _db.CfbReports.Update(entity);
            return await Save();
        }

        public async Task<ICollection<CfbReportTransaction>> GetUnlinkedTransactions(int branchId, DateTime periodStart, DateTime periodEnd)
        {
            return await _db.CfbReportTransactions
                .AsNoTracking()
                .Where(t => t.CfbReportId == null
                         && t.BranchId == branchId
                         && t.TransactionDate >= periodStart
                         && t.TransactionDate <= periodEnd)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<bool> LinkTransactionsToReport(int cfbReportId, IEnumerable<int> transactionIds)
        {
            var transactions = await _db.CfbReportTransactions
                .Where(t => transactionIds.Contains(t.Id) && t.CfbReportId == null)
                .ToListAsync();

            foreach (var txn in transactions)
                txn.CfbReportId = cfbReportId;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
