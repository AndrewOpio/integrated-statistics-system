namespace VMSService.Data.Entities
{
    public class StockMovementDenominationBag
    {
        public int Id { get; set; }
        public int StockMovementDenominationId { get; set; }
        public StockMovementDenomination StockMovementDenomination { get; set; }
        public int BagId { get; set; }
        public Bag Bag { get; set; }
    }
}
