using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class StockLedgerDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int  DenominationId { get; set; }
        public Denomination? Denomination { get; set; }
        public int NumberOfPieces { get; set; }
        public int NumberOfBoxes { get; set; }
        public int? NumberOfBundles { get; set; }
        public decimal Value { get; set; } //Encrypt values here
        public int StockLedgerId { get; set; }
        public StockLedger? StockLedger { get; set; }
    }
}
