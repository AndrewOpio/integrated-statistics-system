using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbReport : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<CfbReportTransaction>? CfbReportTransactions { get; set; }
        public ICollection<CfbReportStatusHistory>? CfbReportStatusHistories { get; set; }
    }
}
