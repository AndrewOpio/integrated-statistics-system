namespace WorkflowService.Client.Dto
{
    /// <summary>
    /// Defines valid workflow transitions between statuses.
    /// This table configures which statuses can transition to which based on user role and action.
    /// </summary>
    public class WorkflowTransitionDto
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Entity type/Category this transition applies to (e.g., "ApprovedCurrencyProcurement")
        /// </summary>
        public int EntityTypeId { get; set; }
        public EntityTypeDto EntityType { get; set; }

        /// <summary>
        /// Current status that this transition starts from
        /// </summary>
        public int FromStatusId { get; set; }
        public StatusTypeDto FromStatus { get; set; }

        /// <summary>
        /// Status to transition to
        /// </summary>
        public int ToStatusId { get; set; }
        public StatusTypeDto ToStatus { get; set; }

        /// <summary>
        /// Action/Button name that triggers this transition (e.g., "Verify", "Reject", "Resubmit")
        /// </summary>
        public string ActionName { get; set; }
        
        /// <summary>
        /// Role required to perform this transition (e.g., "Currency Approver", "Contract Manager")
        /// </summary>
        public int RequiredRole { get; set; }
        
        /// <summary>
        /// Whether this transition requires comments/remarks
        /// </summary>
        public bool RequiresComments { get; set; }

        /// <summary>
        /// Whether this transition requires attachments
        /// </summary>
        public bool RequiresAttachments { get; set; }

        /// <summary>
        /// Collection of attachment requirements for this transition
        /// </summary>
        public ICollection<TransitionAttachmentRequirementDto> AttachmentRequirements { get; set; } = new List<TransitionAttachmentRequirementDto>();
        
        /// <summary>
        /// Display label for the button/action
        /// </summary>
        public string DisplayLabel { get; set; }
        
        /// <summary>
        /// CSS button class (e.g., "btn-success", "btn-danger")
        /// </summary>
        public string ButtonClass { get; set; }

        /// <summary>
        /// Whether to send an email notification when this transition is performed
        /// </summary>
        public bool SendNotification { get; set; }

        /// <summary>
        /// Comma-separated list of role IDs that should receive the notification
        /// </summary>
        public string? NotificationRoleIds { get; set; }

        /// <summary>
        /// Comma-separated list of email addresses that should receive the notification
        /// </summary>
        public string? NotificationEmails { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}