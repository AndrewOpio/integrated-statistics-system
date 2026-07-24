namespace VMSService.Core.Models.Cfb
{
    public class CfbCashHoldingViewModel
    {
        public int Id { get; init; }
        public int CfbBoxId { get; init; }

        public int DenominationId { get; init; }
        /// <summary>Populated by the controller from StockService at display time.</summary>
        public string DenominationName { get; init; } = string.Empty;
        /// <summary>e.g. "Note" or "Coin". Populated by the controller from StockService at display time.</summary>
        public string DenominationTypeName { get; init; } = string.Empty;

        /// <summary>Baseline amount in UGX — the allocation for this denomination in this box.</summary>
        public long Amount { get; init; }

        /// <summary>false for historical (superseded) rows; true for the current baseline.</summary>
        public bool IsActive { get; init; }
    }
}