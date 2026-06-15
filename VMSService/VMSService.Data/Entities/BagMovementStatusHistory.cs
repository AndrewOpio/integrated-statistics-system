using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BagMovementStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int BagMovementId { get; set; }
        public BagMovement? BagMovement { get; set; } = null!;
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; } // Comma Separated Values of AttachmentIds
        public bool IsDeleted { get; set; } = false;
    }
}
