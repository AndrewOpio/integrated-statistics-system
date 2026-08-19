using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BagMovementBag : AuditableEntity
    {
        public int Id { get; set; }
        public int BagMovementId { get; set; }
        public BagMovement? BagMovement { get; set; }
        public int BagId { get; set; }
        public Bag? Bag { get; set; }
        public int OldVaultSectionId { get; set; }
        public VaultSection? OldVaultSection { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
