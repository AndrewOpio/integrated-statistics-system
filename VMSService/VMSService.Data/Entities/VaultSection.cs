using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class VaultSection : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VaultId { get; set; }
        public Vault? Vault { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
