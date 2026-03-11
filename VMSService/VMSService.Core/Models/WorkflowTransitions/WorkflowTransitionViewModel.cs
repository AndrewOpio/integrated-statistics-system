namespace VMSService.Core.Models.WorkflowTransitions
{
    public class WorkflowTransitionViewModel
    {
        public int Id { get; set; }
        public string EntityType { get; set; }
        public int FromStatusId { get; set; }
        public string? FromStatusName { get; set; }
        public int ToStatusId { get; set; }
        public string? ToStatusName { get; set; }
        public string ActionName { get; set; }
        public string RequiredRole { get; set; }
        public bool RequiresComments { get; set; }
        public string DisplayLabel { get; set; }
        public string ButtonClass { get; set; }
        public bool IsActive { get; set; }
    }
}