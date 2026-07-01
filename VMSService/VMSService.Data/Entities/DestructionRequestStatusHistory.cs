using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequestStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int DestructionRequestId { get; set; }
        public DestructionRequest DestructionRequest { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; }
        public bool IsDeleted { get; set; }
    }
}
