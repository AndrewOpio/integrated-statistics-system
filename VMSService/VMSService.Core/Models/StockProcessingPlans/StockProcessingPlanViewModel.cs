namespace VMSService.Core.Models.StockProcessingPlans
{
    public class StockProcessingPlanViewModel
    {
        public int Id { get; set; }
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int? StockMovementId { get; set; }
        public string? StockMovementReference { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
    }
}
