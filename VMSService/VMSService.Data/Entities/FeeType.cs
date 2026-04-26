using StockService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class FeeType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
