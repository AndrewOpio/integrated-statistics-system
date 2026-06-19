using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.EodDiscrepancyReports
{
    public class EodDiscrepancyReportRepository(VMSServiceDbContext _db,
        ILogger<EodDiscrepancyReportRepository> _logger) : IEodDiscrepancyReportRepository
    {
        public async Task<bool> Create(EodDiscrepancyReport entity)
        {
            await _db.EodDiscrepancyReports.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<EodDiscrepancyReport>> FindAll()
        {
            return await _db.EodDiscrepancyReports
                .AsNoTracking()
                .Include(r => r.BoardSessionSurplusShortages)
                .OrderByDescending(r => r.PeriodStart)
                .ToListAsync();
        }

        public async Task<EodDiscrepancyReport> FindById(int id)
        {
            var result = await _db.EodDiscrepancyReports
                .Include(r => r.BoardSessionSurplusShortages)
                    .ThenInclude(s => s.DifferentDenoms)
                .Include(r => r.BoardSessionSurplusShortages)
                    .ThenInclude(s => s.DepositRequest)
                        .ThenInclude(dr => dr.DepositRequestDenominations)
                .Include(r => r.EodDiscrepancyReportStatusHistories)
                .FirstOrDefaultAsync(r => r.Id == id);
            return result!;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.EodDiscrepancyReports.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(EodDiscrepancyReport entity)
        {
            _db.EodDiscrepancyReports.Update(entity);
            return await Save();
        }

        public async Task<bool> UpdateWithChildren(EodDiscrepancyReport entity)
        {
            _db.EodDiscrepancyReports.Update(entity);
            return await Save();
        }

        public async Task<ICollection<BoardSessionSurplusShortage>> GetUnlinkedTransactions(int branchId, DateTime periodStart, DateTime periodEnd)
        {
            return await _db.BoardSessionSurplusShortages
                .AsNoTracking()
                .Where(t => t.EodDiscrepancyReportId == null
                         && t.BranchId == branchId
                         && t.CreatedDate >= periodStart
                         && t.CreatedDate < periodEnd.Date.AddDays(1))
                .Include(t => t.DifferentDenoms)
                .Include(t => t.DepositRequest)
                    .ThenInclude(dr => dr.DepositRequestDenominations)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> LinkTransactionsToReport(int cfbReportId, IEnumerable<int> transactionIds)
        {
            var sessions = await _db.BoardSessionSurplusShortages
                .Where(t => transactionIds.Contains(t.Id) && t.EodDiscrepancyReportId == null)
                .ToListAsync();

            foreach (var session in sessions)
                session.EodDiscrepancyReportId = cfbReportId;

            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
