using WorkflowService.Data.Entities.AuditColumns;

namespace WorkflowService.Data.Entities
{
    public class TransitionAttachmentRequirement : AuditableEntity
    {
        public int Id { get; set; }
        public int WorkflowTransitionId { get; set; }
        public WorkflowTransition? WorkflowTransition { get; set; }
        public string AttachmentName { get; set; }
        public string AllowedFormats { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsRequired { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
