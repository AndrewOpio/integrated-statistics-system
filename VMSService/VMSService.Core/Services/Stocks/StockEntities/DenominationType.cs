using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class DenominationType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public List<Denomination> Denominations { get; set; } = new List<Denomination>();
    }
}
