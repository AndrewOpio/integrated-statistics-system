using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BoardSessionSurplusShortageStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int BoardSessionSurplusShortageId { get; set; }
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; }
        public bool IsDeleted { get; set; }

        public virtual BoardSessionSurplusShortage BoardSessionSurplusShortage { get; set; } = null!;
    }
}
