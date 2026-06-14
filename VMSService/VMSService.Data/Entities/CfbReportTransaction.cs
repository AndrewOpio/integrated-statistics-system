using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbReportTransaction : AuditableEntity
    {
        public int Id { get; set; }
        public int? CfbReportId { get; set; } // null until compiled into a report
        public int BranchId { get; set; }
        public string TellerName { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string? TransactionReference { get; set; }
        public string Type { get; set; } = null!; // "Shortage" or "Surplus"
        public int DenominationId { get; set; }
        public decimal Amount { get; set; }

        public virtual CfbReport? CfbReport { get; set; }
    }
}
