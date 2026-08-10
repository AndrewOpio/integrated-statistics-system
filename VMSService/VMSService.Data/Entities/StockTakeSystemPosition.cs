using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    // A frozen snapshot of the stock ledger's position for one (category, denomination)
    // pair, taken at the moment a StockTake is created — so the variance comparison
    // always reflects what the ledger said on the count date, not whatever it says
    // whenever the record is later viewed.
    public class StockTakeSystemPosition : AuditableEntity
    {
        public int Id { get; set; }
        public int StockTakeId { get; set; }
        public StockTake? StockTake { get; set; }
        public int CategoryId { get; set; }
        public int DenominationId { get; set; }
        public decimal Value { get; set; }
    }
}
