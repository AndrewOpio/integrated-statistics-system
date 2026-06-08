using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Attachment : AuditableEntity
    {
        public long Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int WorkflowTransitionId { get; set; }
        public int TransitionAttachmentRequirementId { get; set; }
        public string? EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;
    }
}
