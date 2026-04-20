using ConfigurationsService.Client;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Models.DepositRequests
{
    public class BouDepositRequestViewModel
    {
        public int PendingCbCount { get; set; }
        public int UnderBouProcessing { get; set; }
        public int SpecialApprovalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int ApprovedCountDiff { get; set; }
        public decimal TotalProcessed { get; set; }
        public string? SelectedBranch { get; set; }
        public bool CanViewAllLocations { get; set; } = false;
        public IEnumerable<BranchDto> BouBranches { get; set; } = new List<BranchDto>();
        public IEnumerable<RecentRequestRowViewModel> Requests { get; set; } = [];
    }
}