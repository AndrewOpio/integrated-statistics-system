using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class StockLedger : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; }
        public bool IsIn { get; set; }
        public int CounterPartyTypeId { get; set; }
        public CounterPartyType? CounterPartyType { get; set; }
        public int CounterPartyId { get; set; }
        public int StoreId { get; set; }
        public Branch? Store { get; set; }
        //public int CategoryId { get; set; }
        //public Category? Category { get; set; }
        public string Notes { get; set; }
        public ICollection<StockLedgerDetail> StockLedgerDetails { get; set; }
    }
}
