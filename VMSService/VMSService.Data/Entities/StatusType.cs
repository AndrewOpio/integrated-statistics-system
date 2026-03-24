using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StatusType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
    }
}
