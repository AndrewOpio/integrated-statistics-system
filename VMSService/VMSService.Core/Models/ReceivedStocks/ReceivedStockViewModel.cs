using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.ExPrintMints;
using VMSService.Core.Models.ReceivedStocksDetails;
using VMSService.Core.Models.ReceivedStockStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.ReceivedStocks
{
    public class ReceivedStockViewModel
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public DateTime DateDelivered { get; set; }
        public int ReceivedStatus { get; set; }
        public string DeliveryAirline { get; set; }
        public decimal TestingFees { get; set; }
        public decimal ShippingFees { get; set; }
        public decimal InsuranceFees { get; set; }
        public decimal HandlingCharges { get; set; }
        public decimal ClearingFees { get; set; }
        public int CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPartial { get; set; }

        public ExPrintMintViewModel? ExPrintMint { get; set; }
        public ICollection<ReceivedStockStatusHistoryViewModel>? StatusHistory { get; set; } = new List<ReceivedStockStatusHistoryViewModel>();
        public ICollection<WorkflowTransitionDto>? AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public ICollection<ReceivedStockDetailViewModel>? Details { get; set; } = new List<ReceivedStockDetailViewModel>();
    }
}
