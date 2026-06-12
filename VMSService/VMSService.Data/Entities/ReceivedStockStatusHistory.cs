using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ReceivedStockStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int ReceivedStockId { get; set; }
        public ReceivedStock ReceivedStock { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; } // Comma Separated Values of AttachmentIds
        public bool IsDeleted { get; set; }
    }
}
