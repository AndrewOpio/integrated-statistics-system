using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class CounterPartyType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<StockLedger> StockLedgers { get; set; }
    }
}
