using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class MinDepositStandard : AuditableEntity
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public int BranchId { get; set; }
        public int MinimumBundles { get; set; }
        public decimal Value { get; set; }
        public bool IsDeleted { get; set; }
    }
}
