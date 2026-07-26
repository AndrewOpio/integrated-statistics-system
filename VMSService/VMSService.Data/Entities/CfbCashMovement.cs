using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbCashMovement : AuditableEntity
    {
        public long Id { get; set; }

        public int CfbBoxId { get; set; }
        public int DenominationId { get; set; }
        // Snapshot of the denomination's face value (UGX) at the time this movement was recorded.
        public int DenomValue { get; set; }

        // Signed change in UGX for this denomination: positive = inflow, negative = outflow.
        // A session settlement may book multiple lines under the same denomination, tied by SourceId.
        public long AmountChange { get; set; }

        public string MovementType { get; set; } = null!;
        public int? SourceId { get; set; }

        public CfbBox CfbBox { get; set; } = null!;

        public DateTime OccurredAt { get; set; } = AppClock.Now;
    }
}