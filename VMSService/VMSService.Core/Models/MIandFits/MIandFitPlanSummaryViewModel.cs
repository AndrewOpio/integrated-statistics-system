namespace VMSService.Core.Models.MIandFits
{
    public class MIandFitPlanSummaryViewModel
    {
        public int StockProcessingPlanId { get; set; }
        public string PlanReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int DenominationCount { get; set; }
        public int PendingCount { get; set; }
    }
}
