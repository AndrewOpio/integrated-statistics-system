namespace WorkflowService.Client.Dto
{
    public class TransitionAttachmentRequirementDto
    {
        public int Id { get; set; }
        public int WorkflowTransitionId { get; set; }
        public string AttachmentName { get; set; } = string.Empty;
        public string AllowedFormats { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsRequired { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
