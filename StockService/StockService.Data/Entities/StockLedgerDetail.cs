using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class StockLedgerDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int  DenominationId { get; set; }
        public Denomination? Denomination { get; set; }
        public int Count { get; set; }
        public int Values { get; set; }
        public int StockLedgerId { get; set; }
        public StockLedger? StockLedger { get; set; }
    }
}
