using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingSessionStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public StockProcessingSession? StockProcessingSession { get; set; }
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? Attachments { get; set; } // Comma Separated Values of AttachmentIds
        public bool IsDeleted { get; set; } = false;
    }
}
