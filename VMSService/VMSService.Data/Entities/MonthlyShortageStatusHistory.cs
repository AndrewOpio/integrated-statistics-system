using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("MonthlyShortageStatusHistories")]
    public class MonthlyShortageStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int MonthlyShortageId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = null!;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; }
        public bool IsDeleted { get; set; }

        public virtual MonthlyShortage MonthlyShortage { get; set; } = null!;
    }
}
