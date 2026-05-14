using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DestructionRequest : AuditableEntity
    {
        public int Id { get; set; }
        public string? TeamLeader { get; set; }
        public string? AssistantTeamLeader { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? ReferenceNumber { get; set; }

    } 
}
