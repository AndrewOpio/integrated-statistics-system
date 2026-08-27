using System.Collections.Generic;
using VMSService.Core.Models.WithdrawalRequests;

namespace VMSService.Core.Models.CbRequests
{
    public class CbRequestHistoryViewModel
    {
        public List<RecentRequestRowViewModel> Rows { get; set; } = [];
        public DateRangeFilterViewModel DateRange { get; set; } = new();
    }
}
