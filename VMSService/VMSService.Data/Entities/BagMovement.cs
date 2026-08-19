using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BagMovement : AuditableEntity
    {
        public int Id { get; set; }
        public int? BagId { get; set; }
        public Bag? Bag { get; set; }
        public int? CurrentStatusId { get; set; }
        public int? OldVaultSectionId { get; set; }
        public VaultSection? OldVaultSection { get; set; }
        public int NewVaultSectionId { get; set; }
        public VaultSection? NewVaultSection { get; set; }
        public ICollection<BagMovementBag> BagMovementBags { get; } = [];
        public bool IsDeleted { get; set; } = false;
    }
}