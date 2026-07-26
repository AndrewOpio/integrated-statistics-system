using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class AppliedPenalty : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>One of PenaltyConstants.Source.*</summary>
        [MaxLength(20)]
        public string Source { get; set; } = default!;

        /// <summary>The withdrawal request's TransactionRefNumber (carries branch).</summary>
        [MaxLength(100)]
        public string SourceId { get; set; } = default!;

        public int PenaltyId { get; set; }
        public Penalty? Penalty { get; set; }

        public decimal ChargeValue { get; set; }
        public decimal? DenomValue { get; set; }
        public int? Quantity { get; set; }

        public bool IsSettledOnRtgs { get; set; } = false;
        public string? RtgsCode { get; set; }
        public string? RtgsSettlingMessage { get; set; }
    }
}
