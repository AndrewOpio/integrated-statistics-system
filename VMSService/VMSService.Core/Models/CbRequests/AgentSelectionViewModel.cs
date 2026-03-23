using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.AgentDetails;

namespace VMSService.Core.Models.CbRequests
{
    public class AgentSelectionViewModel
    {
        public ICollection<AgentDetailViewModel> Agents { get; init; } = [];
    }
}
