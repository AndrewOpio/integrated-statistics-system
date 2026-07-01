using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionAuthorizationDenomination : AuditableEntity
    {
        public int Id { get; set; }
        public int DestructionAuthorizationId { get; set; }
        public int DenominationId { get; set; }
        public string? NumberOfBags { get; set; }
        public decimal Amount { get; set; }

        public virtual DestructionAuthorization DestructionAuthorization { get; set; } = null!;
    }
}
