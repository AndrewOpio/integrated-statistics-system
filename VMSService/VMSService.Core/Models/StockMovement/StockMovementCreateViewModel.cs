using VMSService.Data.Entities;

namespace VMSService.Core.Models.StockMovement
{
    public class StockMovementCreateViewModel
    {
        public string? MovementType { get; set; }

        public int? LogisticsId { get; set; }
        // Branch fields
        public DateTime? BranchDate { get; set; }
        public string? BranchReferenceNumber { get; set; }
        public int? DispatchingBranch { get; set; }
        public int? ReceivingBranch { get; set; }
        public string? Categorization { get; set; }
        public int? BranchTotalBagsBoxes { get; set; }
        public decimal? BranchGrandTotalValue { get; set; }
        public List<StockMovementDenominationViewModel>? DenominationRows { get; set; }

        // Internal fields
        public DateTime? InternalDate { get; set; }
        public string? InternalReferenceNumber { get; set; }
        public int? InternalTotalBoxes { get; set; }
        public decimal? InternalGrandTotalValue { get; set; }
        public List<InternalStockMovementDenominationViewModel>? InternalDenominationRows { get; set; }
    }
}
