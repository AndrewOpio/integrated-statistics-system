using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using VMSService.Core.Models.StatusTypes;
using VMSService.Core.Models.WorkflowTransitions;

namespace VMSService.Core.Models.ReceivedStocks
{
    public class ReceivedStockViewModel
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public ExPrintMintViewModel? ExPrintMint { get; set; }
        public DateTime DateDelivered { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public string BoxNumberPrefix { get; set; }
        public int? BoxNumberRangeStart { get; set; }
        public int? BoxNumberRangeEnd { get; set; }
        public string? SerialNumberPrefix { get; set; }
        public int? SerialNumberRangeStart { get; set; }
        public int? SerialNumberRangeEnd { get; set; }
        public int CurrentStatusId { get; set; }
        public StatusTypeViewModel? CurrentStatus { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPartial { get; set; }
        public int NumberOfBoxes { get; set; }
        public ICollection<ReceivedStockStatusHistoryViewModel>? StatusHistory { get; set; } = new List<ReceivedStockStatusHistoryViewModel>();
        public ICollection<WorkflowTransitionViewModel>? AvailableTransitions { get; set; } = new List<WorkflowTransitionViewModel>();
    }
}
