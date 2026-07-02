using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BagDenomination : AuditableEntity
    {
        public int Id { get; set; }
        public int BagId { get; set; }
        public Bag Bag { get; set; } = null!;
        public Guid DepositRequestDenominationId { get; set; }
        public DepositRequestDenomination DepositRequestDenomination { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public int NumberOfBundlesOrBoxes { get; set; }
    }
}