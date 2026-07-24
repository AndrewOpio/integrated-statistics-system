using ConfigurationsService.Client;

namespace VMSService.Core.Models.Cfb
{
    /// <summary>Branch detail view — shared target for both supervisor (via click) and restricted users (direct landing).</summary>
    public class CfbBranchDetailViewModel
    {
        public int BouBranchId { get; init; }
        public string BouBranchCode { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;

        /// <summary>null if the branch has no Notes box yet.</summary>
        public CfbBoxDetailViewModel? NotesBox { get; init; }

        /// <summary>null if the branch has no Coins box yet.</summary>
        public CfbBoxDetailViewModel? CoinsBox { get; init; }

        /// <summary>Sum of all active baseline amounts across both boxes in UGX.</summary>
        public long BranchBaselineTotal { get; init; }

        /// <summary>Sum of actual amounts (baseline + movements) across both boxes in UGX.</summary>
        public long BranchActualTotal { get; init; }

        /// <summary>true for branch-restricted users; false for supervisors who can see all branches.</summary>
        public bool IsRestricted { get; init; }

        /// <summary>
        /// All BOU branches for supervisor navigation pills — sorted and rendered in the view.
        /// Empty for restricted users (they see a single fixed-branch badge instead).
        /// </summary>
        public IEnumerable<BranchDto> AllBranches { get; init; } = [];
    }
}
