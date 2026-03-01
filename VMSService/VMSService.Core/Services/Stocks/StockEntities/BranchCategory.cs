using VMSService.Data.AuditColumns;
using VMSService.Data.Entities;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class BranchCategory : AuditableEntity
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public Branch? Branch { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
