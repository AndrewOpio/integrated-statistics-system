using VMSService.Data.Entities;

namespace VMSService.Core.Models.StockProcessingPlans
{
    public class PlanBagSummaryResult
    {
        public int TotalNumberOfBagsOrBoxes { get; set; }
        public decimal GrandTotalValue { get; set; }
        public ICollection<StockMovementDenomination> Denominations { get; set; } = [];
    }
}
