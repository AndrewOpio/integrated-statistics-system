using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.WithdrawalRequests;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class WithdrawalPrintViewModel
    {
        public string TransactionRefNumber { get; set; } = string.Empty;
        public DateOnly CollectionDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }
        public string CbName { get; set; } = string.Empty;
        public string BouBranchName { get; set; } = string.Empty;
        public string BouBranchCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TotalNotesAmount { get; set; }
        public decimal TotalCoinsAmount { get; set; }
        public List<AgentDetailViewModel> Agents { get; set; } = [];
        public List<BouDispatchCardViewModel> NewBanknotes { get; set; } = [];
        public List<BouDispatchSimpleViewModel> ReissueBanknotes { get; set; } = [];
        public List<BouDispatchSimpleViewModel> Coins { get; set; } = [];
        public string PanelAName { get; set; } = string.Empty;
        public string PanelBName { get; set; } = string.Empty;
        public bool RequiresSpecialApproval { get; set; }
    }
}
