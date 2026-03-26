using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockMovementStatusHistory : AuditableEntity
    {
        public int Id { get; set; }
        public int StockMovementId { get; set; }
        public StockMovement StockMovement { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
