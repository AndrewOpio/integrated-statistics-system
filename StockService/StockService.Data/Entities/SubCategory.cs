using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class SubCategory : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
