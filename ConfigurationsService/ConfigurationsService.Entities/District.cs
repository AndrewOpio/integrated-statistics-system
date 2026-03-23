using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Entities
{
    public class District : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
