namespace VMSService.Data.Entities
{
    public class BoardSessionDifferentDenom
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public decimal Amount { get; set; }
        public int StockProcessingSessionId { get; set; }
        public virtual StockProcessingSession? StockProcessingSession { get; set; }
    }
}
