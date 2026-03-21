using ConfigurationsService.Data.Entities.AuditColumns;

namespace ConfigurationsService.Data.Entities
{
    public class District : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
