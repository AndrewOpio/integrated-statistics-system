using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Bag : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; } // Denomination constraint
        public bool IsDeleted { get; set; } = false;
        public ICollection<BagDenomination> BagDenominations { get; set; } = [];
    }
}
