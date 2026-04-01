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
    }
}
