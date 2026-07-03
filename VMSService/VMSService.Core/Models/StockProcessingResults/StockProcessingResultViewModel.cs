namespace VMSService.Core.Models.StockProcessingResults
{
    public class StockProcessingResultViewModel
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public string? SessionReferenceNumber { get; set; }
        public int Count { get; set; }
        public decimal FitValue { get; set; }
        public decimal UnfitShredValue { get; set; }
        public decimal FitShredValue { get; set; }
        public decimal CanceledValue { get; set; }
        public decimal ReissueValue { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
