namespace WorkflowService.Core.Models.WorkflowTransitions
{
    public class TransitionAttachmentRequirementViewModel
    {
        public int Id { get; set; }
        public int WorkflowTransitionId { get; set; }
        public string AttachmentName { get; set; } = string.Empty;
        public string AllowedFormats { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsRequired { get; set; } = true;

        public List<string> SelectedFormats { get; set; } = [];

        public List<string> FormatsList => string.IsNullOrWhiteSpace(AllowedFormats)
            ? []
            : AllowedFormats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
