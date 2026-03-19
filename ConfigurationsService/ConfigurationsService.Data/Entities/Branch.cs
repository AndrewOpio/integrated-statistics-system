using ConfigurationsService.Data.Entities.AuditColumns;

namespace ConfigurationsService.Data.Entities
{
    public class Branch : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
