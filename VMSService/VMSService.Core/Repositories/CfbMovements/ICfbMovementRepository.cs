using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbMovements
{
    public interface ICfbMovementRepository
    {
        Task<IReadOnlyList<CfbCashMovement>> GetByBoxIdsAsync(IEnumerable<int> boxIds);

        Task<IReadOnlyList<CfbCashMovement>> GetByBoxIdAsync(int boxId);

        Task<bool> Add(CfbCashMovement entity);
        Task<bool> Save();
    }
}