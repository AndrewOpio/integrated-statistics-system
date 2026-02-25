using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ApprovedCurrencyStatusHistories
{
    public interface IApprovedCurrencyStatusHistoryRepository : IRepositoryBase<ApprovedCurrencyStatusHistory, int>
    {
        Task<ICollection<ApprovedCurrencyStatusHistory>> FindByApprovedCurrencyId(int approvedCurrencyProcurementId);
    }
}