namespace VMSService.Core.Models.StockMovement
{
    public class WorkflowTransitionDto
    {
        public string ActionName { get; set; }
        public string DisplayLabel { get; set; }
        public bool RequiresComments { get; set; }
        public string ButtonClass { get; set; }
    }
}
