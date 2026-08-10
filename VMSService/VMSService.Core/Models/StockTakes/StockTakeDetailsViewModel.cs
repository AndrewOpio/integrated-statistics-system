using VMSService.Core.Models.Shared;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.StockTakes
{
    public class StockTakeDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime CountDate { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public bool IsLocked { get; set; }
        public int? AmendsStockTakeId { get; set; }
        public string? AmendsStockTakeReference { get; set; }
        public int? AmendedByStockTakeId { get; set; }
        public string? AmendedByStockTakeReference { get; set; }

        // Precomputed server-side (mirrors the client-side variance calc) so the header stat
        // strip renders correct numbers immediately, without waiting on the live JS refresh.
        public int LineCount { get; set; }
        public int VaultSectionCount { get; set; }
        public decimal TotalCountedValue { get; set; }
        public decimal TotalSystemValue { get; set; }
        public decimal TotalVariance { get; set; }
        public int MismatchCount { get; set; }

        public ICollection<StockTakeDetailLineViewModel> Lines { get; set; } = new List<StockTakeDetailLineViewModel>();
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public ICollection<StatusHistoryViewModel> StatusHistory { get; set; } = new List<StatusHistoryViewModel>();
    }

    public class StockTakeDetailLineViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int VaultSectionId { get; set; }
        public string VaultSectionName { get; set; } = string.Empty;
        public string VaultName { get; set; } = string.Empty;
        public int DenominationId { get; set; }
        public string DenominationName { get; set; } = string.Empty;
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
    }
}
