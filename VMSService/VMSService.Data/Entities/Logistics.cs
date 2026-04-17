using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Logistics : AuditableEntity
    {
        public int Id { get; set; }
        public string? TeamLeader { get; set; }
        public string? AssistantTeamLeader { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? ReferenceNumber { get; set; }

        // Workflow status tracking
        public int CurrentStatusId { get; set; }
        public virtual StatusType CurrentStatus { get; set; } = null!;

        public virtual ICollection<LogisticsCategory> Categories { get; set; } = new List<LogisticsCategory>();
        public virtual ICollection<LogisticsStatusHistory> StatusHistory { get; set; } = new List<LogisticsStatusHistory>();
    }
}
