using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositRequestRepository : IRepositoryBase<DepositRequest, Guid>
    {
        Task<DepositRequest?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<DepositRequest>> GetByCbAsync(int cbId, CancellationToken ct = default);
        Task<int> CountByCbBranchAndDateAsync(int cbId, int bouBranchId, DateOnly date, CancellationToken ct = default);
        Task<ICollection<DepositRequest>> FindByBouBranchId(int branchId);
        Task<ICollection<DepositRequest>> FindByBouBranchIds(List<int> branchIds);
    }
}