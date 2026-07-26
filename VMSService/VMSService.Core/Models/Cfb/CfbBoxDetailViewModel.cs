namespace VMSService.Core.Models.Cfb
{
    /// <summary>One physical box (Notes or Coins) inside the branch detail view.</summary>
    public class CfbBoxDetailViewModel
    {
        public int BoxId { get; init; }
        /// <summary>"Note" or "Coin" — matches CfbConstants.BoxTypes / StatusConstants.DenominationName.</summary>
        public string BoxType { get; init; } = string.Empty;
        public string BoxName { get; init; } = string.Empty;

        public IEnumerable<CfbHoldingLineViewModel> Lines { get; init; } = [];

        /// <summary>Sum of all active baseline amounts in UGX.</summary>
        public long BaselineTotal { get; init; }

        /// <summary>Sum of all actual amounts (baseline + movements) in UGX.</summary>
        public long ActualTotal { get; init; }

        /// <summary>ActualTotal − BaselineTotal. Positive = net surplus; negative = net shortfall.</summary>
        public long Deviation => ActualTotal - BaselineTotal;

        /// <summary>True when at least one denomination for this box type has no active baseline yet.</summary>
        public bool HasUnsetDenominations { get; init; }
    }
}