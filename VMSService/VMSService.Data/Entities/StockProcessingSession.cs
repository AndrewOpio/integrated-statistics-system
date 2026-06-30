using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingSession : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int StockProcessingPlanId { get; set; }
        public StockProcessingPlan? StockProcessingPlan { get; set; }
        public int CommercialBankId { get; set; }
        public int CategoryId { get; set; }
        public int? ProcessingBoardId { get; set; }
        public ProcessingBoard? ProcessingBoard { get; set; }
        public int CurrentStatusId { get; set; }
        public bool IsManual { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public ICollection<ProcessingSessionBag> ProcessingSessionBags { get; set; } = [];
    }
}
