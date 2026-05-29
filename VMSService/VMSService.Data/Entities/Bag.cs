using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Bag : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public Guid DepositRequestDenominationId { get; set; }
        public DepositRequestDenomination? DepositRequestDenomination { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
