using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Entities
{
    public class CommercialBank : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
