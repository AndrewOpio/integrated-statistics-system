using ConfigurationsService.Client;

namespace VMSService.Core.Models.Cfb
{
    /// <summary>Supervisor landing view — all branches, note columns across top, coins total column.</summary>
    public class CfbOverviewViewModel
    {
        /// <summary>Note denomination columns, sorted ascending by face value.</summary>
        public IEnumerable<CfbDenomColumnDef> NoteDenomColumns { get; init; } = [];

        /// <summary>Branches that have at least one box — shown in the main grid.</summary>
        public IEnumerable<CfbOverviewRowViewModel> BranchRows { get; init; } = [];

        /// <summary>All BOU branches — used to render the location pills.</summary>
        public IEnumerable<BranchDto> AllBranches { get; init; } = [];

        /// <summary>Branches that have no boxes at all — shown in the pending-setup section.</summary>
        public IEnumerable<BranchDto> BranchesWithoutBoxes { get; init; } = [];

        /// <summary>Footer: baseline UGX total per note DenominationId across all branches.</summary>
        public Dictionary<int, long> FooterNoteBaselines { get; init; } = [];

        /// <summary>Footer: actual UGX total per note DenominationId across all branches.</summary>
        public Dictionary<int, long> FooterNoteActuals { get; init; } = [];

        public long FooterCoinsBaseline { get; init; }
        public long FooterCoinsActual { get; init; }
        public long FooterGrandBaseline { get; init; }
        public long FooterGrandActual { get; init; }
    }
}
