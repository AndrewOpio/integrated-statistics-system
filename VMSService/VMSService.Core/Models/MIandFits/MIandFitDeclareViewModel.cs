namespace VMSService.Core.Models.MIandFits
{
    public class MIandFitDeclareViewModel
    {
        public int StockProcessingPlanId { get; set; }
        public string PlanReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public List<MIandFitViewModel> Rows { get; set; } = new();
    }

    public class MIandFitReturnEntry
    {
        public int Id { get; set; }
        public decimal ReturnedFitValue { get; set; }
        public decimal ReturnedUnFitValue { get; set; }
    }
}
