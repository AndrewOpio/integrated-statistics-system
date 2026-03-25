using WorkflowService.Data.Entities.AuditColumns;

namespace WorkflowService.Data.Entities
{
    public class EntityType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ApplicationId { get; set; }
    }
}
