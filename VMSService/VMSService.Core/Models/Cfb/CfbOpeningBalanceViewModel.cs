namespace VMSService.Core.Models.Cfb
{
    public class CfbOpeningBalanceViewModel
    {
        public int BoxId { get; init; }
        public string BoxType { get; init; } = string.Empty;
        public string BoxName { get; init; } = string.Empty;
        public int BouBranchId { get; init; }
        public string BouBranchName { get; init; } = string.Empty;
        public IReadOnlyList<CfbOpeningBalanceDenomLineViewModel> Lines { get; init; } = [];

        public bool IsFullyInitialised => Lines.All(l => l.IsSet);
        public int UnsetCount => Lines.Count(l => !l.IsSet);
    }

    /// <summary>Posted from the opening balance form — one entry per denomination input field.</summary>
    public class CfbOpeningBalanceInputModel
    {
        public int BoxId { get; set; }
        public int BouBranchId { get; set; }

        /// <summary>
        /// Keyed by DenominationId. Only denominations without an existing active baseline
        /// appear as editable inputs; the form does not submit entries for already-set denominations.
        /// </summary>
        public Dictionary<int, long?> Amounts { get; set; } = [];
    }
}
