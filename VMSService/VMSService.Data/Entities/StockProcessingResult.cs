using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingResult : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public StockProcessingSession? StockProcessingSession { get; set; }
        public int Count { get; set; }
        public decimal FitValue { get; set; } // From Machine
        public decimal UnfitShredValue { get; set; } // From Machine
        public decimal FitShredValue { get; set; } // From Machine // Should we combine Shredded together?
        public decimal CanceledValue { get; set; } // From Manual
        public decimal ReissueValue { get; set; } // From Manual
        public bool IsDeleted { get; set; } = false;
    }
}
