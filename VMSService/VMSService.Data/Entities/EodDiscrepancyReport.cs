using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("EodDiscrepancyReports")]
    public class EodDiscrepancyReport : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<BoardSessionSurplusShortage>? BoardSessionSurplusShortages { get; set; }
        public ICollection<EodDiscrepancyReportStatusHistory>? EodDiscrepancyReportStatusHistories { get; set; }
    }
}
