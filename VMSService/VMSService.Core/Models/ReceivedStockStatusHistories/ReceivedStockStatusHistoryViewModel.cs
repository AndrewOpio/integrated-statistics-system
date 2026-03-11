using VMSService.Core.Models.ReceivedStocks;
using VMSService.Core.Models.StatusTypes;
using VMSService.Data.AuditColumns;

namespace VMSService.Core.Models.ReceivedStockStatusHistories
{
    public class ReceivedStockStatusHistoryViewModel
    {
        public int Id { get; set; }
        public int ReceivedStockId { get; set; }
        public ReceivedStockViewModel? ReceivedStock { get; set; }
        public int OldStatusId { get; set; }
        public StatusTypeViewModel OldStatus { get; set; }
        public int NewStatusId { get; set; }
        public StatusTypeViewModel NewStatus { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
