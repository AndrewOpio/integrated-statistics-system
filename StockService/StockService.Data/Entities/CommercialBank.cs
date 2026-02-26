using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class CommercialBank : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
