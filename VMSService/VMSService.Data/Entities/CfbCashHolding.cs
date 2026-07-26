using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbCashHolding : AuditableEntity
    {
        public int Id { get; set; }

        public int CfbBoxId { get; set; }
        public CfbBox CfbBox { get; set; } = null!;

        public int DenominationId { get; set; }
        // Snapshot of the denomination's face value (UGX) at the time this row was written.
        // Never recomputed from the live microservice — frozen so math never drifts if the service changes.
        public int DenomValue { get; set; }

        // The allocated/expected amount in UGX for this denomination in this box.
        // Opening balance = first active row. Top-up = deactivate this row, insert new active row with higher amount.
        public long Amount { get; set; }

        // Exactly one active row per (CfbBoxId, DenominationId) — enforced by a filtered unique index.
        // Inactive rows are the baseline history (dated via AuditableEntity).
        public bool IsActive { get; set; }
    }
}