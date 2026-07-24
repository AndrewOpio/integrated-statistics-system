namespace VMSService.Core.Models.Cfb
{
    /// <summary>One row in the supervisor overview grid — one BOU branch.</summary>
    public class CfbOverviewRowViewModel
    {
        public int BouBranchId { get; init; }
        public string BouBranchCode { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;

        /// <summary>Active baseline amount per note DenominationId.</summary>
        public Dictionary<int, long> NoteBaselines { get; init; } = [];

        /// <summary>Actual amount (baseline + movements) per note DenominationId.</summary>
        public Dictionary<int, long> NoteActuals { get; init; } = [];

        public long TotalNotesBaseline { get; init; }
        public long TotalNotesActual { get; init; }

        public long TotalCoinsBaseline { get; init; }
        public long TotalCoinsActual { get; init; }

        public long TotalBaseline { get; init; }
        public long TotalActual { get; init; }

        // TODO: set to true when a pending top-up exists for this branch (wire once top-up tables are available).
        public bool HasPendingTopUp { get; init; }

        /// <summary>true when one of the two required boxes (Notes/Coins) has not been created yet.</summary>
        public bool HasIncompleteSetup { get; init; }
    }
}