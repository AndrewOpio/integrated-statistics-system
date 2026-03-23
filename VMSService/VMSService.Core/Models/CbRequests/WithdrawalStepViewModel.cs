using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMSService.Core.Models.CbRequests
{
    public class WithdrawalStepViewModel
    {
        public IReadOnlyList<WithdrawalStep> Steps { get; init; } = [];
        public bool IsInProgress { get; init; }  // request is with the BOU
        public bool IsApproved { get; init; }    // request fully approved
        public bool ShowBouPill { get; init; }
    }
}
