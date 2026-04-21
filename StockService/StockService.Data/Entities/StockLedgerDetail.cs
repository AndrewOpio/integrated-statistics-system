using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class StockLedgerDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int  DenominationId { get; set; }
        public Denomination? Denomination { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int NumberOfPieces { get; set; }
        public int NumberOfBoxes { get; set; }
        public int? NumberOfBundles { get; set; }
        public string Value { get; set; } //Encrypt values here
        public int StockLedgerId { get; set; }
        public StockLedger? StockLedger { get; set; }
    }
}
