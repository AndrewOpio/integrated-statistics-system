using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class CbRequestsIndexViewModel
    {
        public IEnumerable<RecentRequestRowViewModel> RecentRequests { get; set; } = [];

        public int AwaitingActionCount { get; set; }
        public int PendingExceptionalApprovalCount { get; set; }
        public int WithBouCount { get; set; }
        public int UnsuccessfulCount { get; set; }
    }
}
