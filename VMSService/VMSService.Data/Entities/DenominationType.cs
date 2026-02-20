using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DenominationType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}
