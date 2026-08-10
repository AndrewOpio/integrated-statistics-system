using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockTakeStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int StockTakeId { get; set; }
        public StockTake? StockTake { get; set; }
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
