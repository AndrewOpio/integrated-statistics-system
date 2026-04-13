using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Models.WithdrawalRequests
{
    public class BouWithdrawalRequestViewModel
    {
        public int PendingCbCount { get; set; }
        public int UnderBouProcessing { get; set; }
        public int SpecialApprovalCount { get; set; } //
        public int ApprovedCount { get; set; }
        public int ApprovedCountDiff { get; set; }
        public decimal TotalProcessed { get; set; }
        public IEnumerable<RecentRequestRowViewModel> Requests { get; set; } = [];
    }
}
