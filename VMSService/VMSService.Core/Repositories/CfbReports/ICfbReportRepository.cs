using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbReports
{
    public interface ICfbReportRepository : IRepositoryBase<CfbReport, int>
    {
        Task<bool> UpdateWithChildren(CfbReport entity);
        Task<ICollection<CfbReportTransaction>> GetUnlinkedTransactions(int branchId, DateTime periodStart, DateTime periodEnd);
        Task<bool> LinkTransactionsToReport(int cfbReportId, IEnumerable<int> transactionIds);
    }
}
