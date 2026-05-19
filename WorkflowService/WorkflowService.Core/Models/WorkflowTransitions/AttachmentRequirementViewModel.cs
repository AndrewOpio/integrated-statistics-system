namespace WorkflowService.Core.Models.WorkflowTransitions
{
    public class AttachmentRequirementViewModel
    {
        public int Id { get; set; }
        public int WorkflowTransitionId { get; set; }
        public string AttachmentName { get; set; } = string.Empty;
        public string AllowedFormats { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Parsed list of selected formats for UI binding
        /// </summary>
        public List<string> SelectedFormats { get; set; } = new List<string>();
        
        /// <summary>
        /// Helper property for displaying formats as badges
        /// </summary>
        public List<string> FormatsList => string.IsNullOrWhiteSpace(AllowedFormats) 
            ? new List<string>() 
            : AllowedFormats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}