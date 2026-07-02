using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("EodDiscrepancyReportStatusHistories")]
    public class EodDiscrepancyReportStatusHistory : AuditableEntity
    {
        public int Id { get; set; }

        [Column("EodDiscrepancyReportId")]
        public int EodDiscrepancyReportId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = null!;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }

        public virtual EodDiscrepancyReport EodDiscrepancyReport { get; set; } = null!;
    }
}
