using WorkflowService.Data.Entities.AuditColumns;

namespace WorkflowService.Data.Entities
{
    public class StatusType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int EntityTypeId { get; set; }
        public EntityType EntityType { get; set; }
    }
}
