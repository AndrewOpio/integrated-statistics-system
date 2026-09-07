using VMSService.Data.Entities;

namespace VMSService.Core.Services.StockPositions
{
    public interface IStockPositionService
    {
        /// <summary>
        /// Update stock position based on entity type and status transition.
        /// ErrorMessage is populated whenever Success is false, so callers can surface the real cause.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> UpdatePositionAsync(string entityType, string statusName, object entity);

        /// <summary>
        /// Get current stock position for a branch category and denomination
        /// </summary>
        Task<int> GetStockPositionAsync(int branchCategoryId, int denominationId);

        /// <summary>
        /// Get stock position matrix for a branch (denominations x categories) with values
        /// </summary>
        Task<Dictionary<int, Dictionary<int, decimal>>> GetStockMatrixAsync(int branchId, List<int> categoryIds, List<int> denominationIds);

        /// <summary>
        /// Post a CFB stock ledger entry (IsIn = true) when a shortage reimbursement is approved
        /// </summary>
        Task<bool> CreateReimbursementLedgerAsync(MonthlyShortageReimbursement reimbursement);

        /// <summary>
        /// Compares an amendment's counted lines against the current stock ledger position and
        /// posts adjusting ledger entries so the system position matches the amended count.
        /// </summary>
        Task<bool> ReconcileAmendmentLedgerAsync(StockTake amendment);
    }
}
