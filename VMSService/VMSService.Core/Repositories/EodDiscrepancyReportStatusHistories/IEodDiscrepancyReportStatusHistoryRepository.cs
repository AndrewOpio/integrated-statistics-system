using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories
{
    public interface IEodDiscrepancyReportStatusHistoryRepository : IRepositoryBase<EodDiscrepancyReportStatusHistory, int>
    {
        Task<ICollection<EodDiscrepancyReportStatusHistory>> FindByCfbReportId(int cfbReportId);
    }
}
