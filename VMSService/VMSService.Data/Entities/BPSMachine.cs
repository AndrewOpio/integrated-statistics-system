using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BPSMachine : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentStatusId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
