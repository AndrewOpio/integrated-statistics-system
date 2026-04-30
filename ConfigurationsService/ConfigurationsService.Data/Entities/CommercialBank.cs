using ConfigurationsService.Data.Entities.AuditColumns;

namespace ConfigurationsService.Data.Entities
{
    public class CommercialBank : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string CBCode { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
