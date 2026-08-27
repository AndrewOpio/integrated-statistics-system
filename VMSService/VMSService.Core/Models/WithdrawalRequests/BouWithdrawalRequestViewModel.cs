
using ConfigurationsService.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.Branches;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Models.WithdrawalRequests
{
    public class BouWithdrawalRequestViewModel
    {
        // Hero card aggregates
        public int PendingCbCount { get; set; }
        public int UnderBouProcessing { get; set; }
        public int RecallCount { get; set; }
        public decimal TotalProcessed { get; set; }

        // Individual status counts
        public int PendingInternalApprovalCount { get; set; }
        public int PendingWithdrawalConfirmationCount { get; set; }
        public int PendingExceptionalApprovalCount { get; set; }
        public int PendingRecommendationCount { get; set; }
        public int PendingConfirmationCount { get; set; }
        public int WithdrawalConfirmedCount { get; set; }
        public int RecallRequestedCount { get; set; }
        public int RecalledCount { get; set; }
        public int PendingRtgsVerificationCount { get; set; }
        public int ReturnRtgsPendingCount { get; set; }

        public string? SelectedBranch { get; set; }
        public bool CanViewAllLocations { get; set; } = false;
        // True for a branch-scoped BOU user (e.g. Panel A/B) whose role carries no resolvable
        // branch assignment — a misconfiguration, not "no requests today".
        public bool NotAttachedToBranch { get; set; } = false;
        public BranchViewModel UserBranch { get; set; } = new();
        public IEnumerable<BranchDto> BouBranches { get; set; } = new List<BranchDto>();

        // History's date-range filter — set only on History, unused on Index. No floor: BOU is
        // the authoritative side and can pull any period (unlike the CB-facing equivalent).
        public DateRangeFilterViewModel? DateRange { get; set; }

        public IEnumerable<RecentRequestRowViewModel> Requests { get; set; } = [];
    }
}
