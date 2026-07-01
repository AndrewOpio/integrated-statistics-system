using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.EodDiscrepancyReports
{
    public interface IEodDiscrepancyReportRepository : IRepositoryBase<EodDiscrepancyReport, int>
    {
        Task<bool> UpdateWithChildren(EodDiscrepancyReport entity);
        Task<ICollection<BoardSessionSurplusShortage>> GetUnlinkedTransactions(int branchId, DateTime sessionDate);
        Task<bool> LinkTransactionsToReport(int cfbReportId, IEnumerable<int> transactionIds);
    }
}
