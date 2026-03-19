namespace VMSService.Core.Models.StockMovement
{
    public class StockMovementDetailViewModel
    {
        public VMSService.Data.Entities.StockMovement Movement { get; set; }
        public List<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
    }
}
