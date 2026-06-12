using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Bag : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; } // Denomination constraint
        public bool IsDeleted { get; set; } = false;
        public int BranchId { get; set; }
        public int? VaultSectionId { get; set; }
        public VaultSection? VaultSection { get; set; }
        public ICollection<BagDenomination> BagDenominations { get; set; } = [];
        
        // Self-referencing relationship for nested bags
        public int? ParentBagId { get; set; }
        public Bag? ParentBag { get; set; }
        public ICollection<Bag> ChildBags { get; set; } = [];
    }
}
