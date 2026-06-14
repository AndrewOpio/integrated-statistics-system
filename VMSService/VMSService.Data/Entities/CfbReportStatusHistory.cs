using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CfbReportStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int CfbReportId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = null!;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }

        public virtual CfbReport CfbReport { get; set; } = null!;
    }
}
