using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class Category : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Classification { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ICollection<BranchCategory> BranchCategories { get; set; }
        public ICollection<StockLedger> StockLedgers { get; set; }
    }
}
