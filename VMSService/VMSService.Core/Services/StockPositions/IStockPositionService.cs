using VMSService.Data.Entities;

namespace VMSService.Core.Services.StockPositions
{
    public interface IStockPositionService
    {
        /// <summary>
        /// Update stock position based on entity type and status transition
        /// </summary>
        Task<bool> UpdatePositionAsync(string entityType, string statusName, object entity);

        /// <summary>
        /// Get current stock position for a branch category and denomination
        /// </summary>
        Task<int> GetStockPositionAsync(int branchCategoryId, int denominationId);

        /// <summary>
        /// Get stock position matrix for a branch (denominations x categories) with values
        /// </summary>
        Task<Dictionary<int, Dictionary<int, decimal>>> GetStockMatrixAsync(int branchId, List<int> categoryIds, List<int> denominationIds);
    }
}
