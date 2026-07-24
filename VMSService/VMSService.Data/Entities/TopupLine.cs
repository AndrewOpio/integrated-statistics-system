using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class TopupLine : AuditableEntity
    {
        public int Id { get; set; }
        public int TopupRequestId { get; set; }
        public TopupRequest TopupRequest { get; set; } = null!;

        public int CfbBoxId { get; set; }
        public int DenominationId { get; set; }
        public int DenomValue { get; set; }

        /// <summary>Increment amount to add to the current baseline — never a target value.</summary>
        public long Amount { get; set; }

        /// <summary>Baseline as it was when the supervisor created this request, before approval changes it.</summary>
        public long BaselineBeforeTopup { get; set; }
    }
}
