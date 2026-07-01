using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ProcessingSessionBag : AuditableEntity
    {
        public int Id { get; set; }
        public int BagId { get; set; }
        public Bag Bag { get; set; } = null!;
        public int StockProcessingSessionId { get; set; }
        public StockProcessingSession? StockProcessingSession { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}