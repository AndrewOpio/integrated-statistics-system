using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingPlan : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public int? StockMovementId { get; set; }
        public StockMovement? StockMovement { get; set; }
        public int CurrentStatusId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
