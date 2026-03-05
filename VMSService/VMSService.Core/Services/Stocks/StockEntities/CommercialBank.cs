using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class CommercialBank : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
