using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbReportStatusHistories
{
    public interface ICfbReportStatusHistoryRepository : IRepositoryBase<CfbReportStatusHistory, int>
    {
        Task<ICollection<CfbReportStatusHistory>> FindByCfbReportId(int cfbReportId);
    }
}
