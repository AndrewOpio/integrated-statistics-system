namespace VMSService.Core.Models.Cfb
{
    public class CfbBoxViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;

        /// <summary>"Notes" or "Coins" — see CfbConstants.BoxTypes.</summary>
        public string BoxType { get; init; } = string.Empty;

        public int BouBranchId { get; init; }
        public string BouBranchCode { get; init; } = string.Empty;
        /// <summary>
        /// Populated by the controller from ConfigurationsService.
        /// Falls back to BouBranchCode if the branch service is unavailable at display time.
        /// </summary>
        public string BouBranchName { get; init; } = string.Empty;

        public IEnumerable<CfbCashHoldingViewModel> Holdings { get; init; } = [];

        /// <summary>Sum of active baseline amounts in UGX.</summary>
        public long TotalBaselineAmount { get; init; }
    }
}
