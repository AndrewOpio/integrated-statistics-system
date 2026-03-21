namespace VMSService.Core.Models.StockMovement
{
    public class ReceivedDenominationDto
    {
        public int SentDenominationId { get; set; }
        public int? ReceivedBagsBoxes { get; set; }
        public int? ReceivedBundles { get; set; }
        public int? CategoryId { get; set; }
        public decimal SubTotalValue { get; set; }
    }
}
