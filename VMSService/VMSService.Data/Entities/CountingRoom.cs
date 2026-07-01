using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CountingRoom : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
