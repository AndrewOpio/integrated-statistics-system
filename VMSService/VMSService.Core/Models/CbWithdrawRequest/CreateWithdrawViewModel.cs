using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class CreateWithdrawViewModel
    {
        public WithdrawalStepViewModel Steps { get; init; } = null!;
        public BranchSelectionViewModel Branches { get; init; } = null!;
        public AgentSelectionViewModel Agents { get; init; } = null!;
        public DenomTableViewModel Denoms { get; init; } = null!;
    }
}
