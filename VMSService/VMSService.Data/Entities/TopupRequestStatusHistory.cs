using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class TopupRequestStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int TopupRequestId { get; set; }
        public TopupRequest TopupRequest { get; set; } = null!;

        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = string.Empty;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = string.Empty;

        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
