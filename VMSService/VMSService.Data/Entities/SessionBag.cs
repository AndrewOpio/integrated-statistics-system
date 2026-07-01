using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class SessionBag : AuditableEntity
    {
        public int Id { get; set; }
        public int BagId { get; set; }
        public Bag Bag { get; set; } = null!;
        public int StockProcessingPlanId { get; set; }
        public StockProcessingPlan? StockProcessingPlan { get; set; }
        public int ProcessingBoardId { get; set; }
        public ProcessingBoard? ProcessingBoard { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}