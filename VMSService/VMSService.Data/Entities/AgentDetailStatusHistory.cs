using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class AgentDetailStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int AgentDetailId { get; set; }
        public AgentDetail AgentDetail { get; set; }
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
