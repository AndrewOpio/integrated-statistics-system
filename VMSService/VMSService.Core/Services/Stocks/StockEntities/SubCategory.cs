using StockService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
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
