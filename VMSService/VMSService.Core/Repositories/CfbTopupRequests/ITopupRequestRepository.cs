using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbTopupRequests
{
    public interface ITopupRequestRepository : IRepositoryBase<TopupRequest, int>
    {
        Task<TopupRequest?> FindByIdWithLines(int id);
        Task<ICollection<TopupRequest>> FindByBranchId(int bouBranchId);
        Task<bool> HasPendingForBranch(int bouBranchId);
    }
}
