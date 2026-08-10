using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockProcessingResult : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public StockProcessingSession? StockProcessingSession { get; set; }
        public int Count { get; set; } // From Machine only
        public decimal FitValue { get; set; } // From Machine only
        public decimal UnFitShredValue { get; set; } // From Machine only
        public decimal FitShredValue { get; set; } // From Machine only
        public decimal CanceledValue { get; set; }
        public decimal ReissueValue { get; set; }
        public decimal UnProcessedValue { get; set; } // Money with Boards - The balance that is not processed
        public bool IsDeleted { get; set; } = false;
        public bool IsVerified { get; set; } = false;
    }
}
