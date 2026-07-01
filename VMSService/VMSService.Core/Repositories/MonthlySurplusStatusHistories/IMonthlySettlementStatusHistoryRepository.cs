using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlySurplusStatusHistories
{
    public interface IMonthlySurplusStatusHistoryRepository : IRepositoryBase<MonthlySurplusStatusHistory, int>
    {
        Task<ICollection<MonthlySurplusStatusHistory>> FindByMonthlySurplusId(int monthlySurplusId);
    }
}
