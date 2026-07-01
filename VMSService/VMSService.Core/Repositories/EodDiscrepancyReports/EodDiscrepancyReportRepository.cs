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
                .Include(r => r.Entries)
                    .ThenInclude(e => e.BoardSessionSurplusShortage)
                .OrderByDescending(r => r.SessionDate)
                .ToListAsync();
        }

        public async Task<EodDiscrepancyReport> FindById(int id)
        {
            var result = await _db.EodDiscrepancyReports
                .Include(r => r.Entries)
                    .ThenInclude(e => e.BoardSessionSurplusShortage)
                        .ThenInclude(s => s.DifferentDenoms)
                .Include(r => r.Entries)
                    .ThenInclude(e => e.BoardSessionSurplusShortage)
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

        public async Task<ICollection<BoardSessionSurplusShortage>> GetUnlinkedTransactions(int branchId, DateTime sessionDate)
        {
            var dayStart = sessionDate.Date;
            var dayEnd   = dayStart.AddDays(1);
            return await _db.BoardSessionSurplusShortages
                .AsNoTracking()
                .Where(t => !_db.EodDiscrepancyReportEntries.Any(e => e.BoardSessionSurplusShortageId == t.Id)
                         && t.BranchId == branchId
                         && t.CreatedDate >= dayStart
                         && t.CreatedDate < dayEnd)
                .Include(t => t.DifferentDenoms)
                .Include(t => t.DepositRequest)
                    .ThenInclude(dr => dr.DepositRequestDenominations)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> LinkTransactionsToReport(int cfbReportId, IEnumerable<int> transactionIds)
        {
            var alreadyLinked = await _db.EodDiscrepancyReportEntries
                .Where(e => transactionIds.Contains(e.BoardSessionSurplusShortageId))
                .Select(e => e.BoardSessionSurplusShortageId)
                .ToListAsync();

            var newEntries = transactionIds
                .Except(alreadyLinked)
                .Select(id => new EodDiscrepancyReportEntry
                {
                    EodDiscrepancyReportId        = cfbReportId,
                    BoardSessionSurplusShortageId = id,
                });

            await _db.EodDiscrepancyReportEntries.AddRangeAsync(newEntries);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
