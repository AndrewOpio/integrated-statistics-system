namespace VMSService.Core.Models.ReceivedStocksDetails
{
    public class ReceivedStockDetailViewModel
    {
        public int Id { get; set; }
        public int ReceivedStockId { get; set; }
        public int PackingListRangeId { get; set; }
        public string? DenominationName { get; set; } // Populated dynamically in controller
        public int ReceivedStartCaseNumber { get; set; }
        public int ReceivedEndCaseNumber { get; set; }
        public bool IsDeleted { get; set; }
    }
}
