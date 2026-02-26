using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ReceivedStockStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int ReceivedStockId { get; set; }
        public ReceivedStock ReceivedStock { get; set; }
        public int OldStatusId { get; set; }
        public StatusType OldStatus { get; set; }
        public int NewStatusId { get; set; }
        public StatusType NewStatus { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
