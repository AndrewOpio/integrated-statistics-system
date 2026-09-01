using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DestructionAuthorizations
{
    public interface IDestructionAuthorizationRepository : IRepositoryBase<DestructionAuthorization, int>
    {
        Task<bool> UpdateWithChildren(DestructionAuthorization entity);
        Task<ICollection<DestructionAuthorization>> GetApprovedAuthorizations();
        Task<Dictionary<int, decimal>> GetRemainingAmounts(int authorizationId, int? excludeRequestId = null);
        Task<Dictionary<int, int>> GetRemainingBags(int authorizationId, int? excludeRequestId = null);
        Task<DestructionAuthorization?> FindActiveByBranch(int branchId, int? excludeId = null);
    }
}
