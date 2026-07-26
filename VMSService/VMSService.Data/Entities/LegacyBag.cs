using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class LegacyBag : AuditableEntity
    {
        public int Id { get; set; }
        public DateOnly DepositDate { get; set; }
        public int SourceDepositBranchId { get; set; }
        public decimal Value { get; set; }
    }
}
