using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionLocation : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
    }
}
