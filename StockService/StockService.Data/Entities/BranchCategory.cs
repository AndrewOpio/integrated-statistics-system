using StockService.Data.AuditColumns;

namespace StockService.Data.Entities
{
    public class BranchCategory : AuditableEntity
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
