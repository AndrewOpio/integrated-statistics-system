namespace WorkflowService.Client.Dto
{
    /// <summary>
    /// Defines attachment requirements for a workflow transition.
    /// Each transition can have multiple required attachments with specific format requirements.
    /// </summary>
    public class TransitionAttachmentRequirementDto
    {
        public int Id { get; set; }
        
        /// <summary>
        /// The workflow transition this attachment requirement belongs to
        /// </summary>
        public int WorkflowTransitionId { get; set; }
        //public WorkflowTransitionDto WorkflowTransition { get; set; }
        
        /// <summary>
        /// Display name for the attachment (e.g., "Approval Letter", "Supporting Documents")
        /// </summary>
        public string AttachmentName { get; set; }
        
        /// <summary>
        /// Comma-separated list of allowed file formats (e.g., "pdf,excel,docx,png,jpg")
        /// </summary>
        public string AllowedFormats { get; set; }
        
        /// <summary>
        /// Display order for the attachment in the UI
        /// </summary>
        public int DisplayOrder { get; set; }
        
        /// <summary>
        /// Whether this attachment is mandatory
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        public bool IsDeleted { get; set; }
    }
}
