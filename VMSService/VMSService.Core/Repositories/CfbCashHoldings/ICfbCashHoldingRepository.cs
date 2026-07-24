using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbCashHoldings
{
    public interface ICfbCashHoldingRepository
    {
        Task<ICollection<CfbCashHolding>> FindAll();
        Task<ICollection<CfbCashHolding>> FindByCfbBoxId(int cfbBoxId, bool activeOnly = true);
        Task<CfbCashHolding?> FindById(int id);
        Task<bool> IsExists(int id);

        /// <summary>
        /// Sets the active baseline for a denomination in a box.
        /// If an active row already exists it is deactivated first (baseline history is retained).
        /// Then a new active row is inserted with the given amount.
        /// Call Save() after to commit atomically.
        /// TODO: top-up / destruction path — wire the existing-row branch to the approval workflow
        ///       service before calling this so the caller has confirmed the workflow has approved.
        /// </summary>
        Task SetBaseline(int cfbBoxId, int denominationId, long amount);

        /// <summary>
        /// Sets the first-ever baseline (opening balance) for one or more denominations in a box.
        /// Only denominations that do not yet have an active holding are processed.
        /// Throws if any provided denomination already has an active row (guard against double-init).
        /// Entries with Amount ≤ 0 are silently skipped — every denomination must eventually be set,
        /// but partial submissions are allowed; come back for the rest via the same action.
        /// Does NOT write any CfbCashMovement rows — opening balance is purely a baseline.
        /// Call Save() after to commit all inserts atomically.
        /// </summary>
        Task SetOpeningBalance(int cfbBoxId, IReadOnlyDictionary<int, long> amountsByDenomId);

        Task<CfbCashHolding?> FindActiveByBoxAndDenom(int cfbBoxId, int denominationId);
        Task Create(CfbCashHolding holding);
        Task<bool> Save();
    }
}