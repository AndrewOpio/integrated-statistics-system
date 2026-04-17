using System;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class LogisticsStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int LogisticsId { get; set; }
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public virtual Logistics Logistics { get; set; } = null!;
        public virtual StatusType OldStatus { get; set; } = null!;
        public virtual StatusType NewStatus { get; set; } = null!;
    }
}
