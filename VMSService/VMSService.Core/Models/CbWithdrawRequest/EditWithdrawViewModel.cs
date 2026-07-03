using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    // ── Request identity ──────────────────────
    public class EditWithdrawViewModel
    {
        public WithdrawalRequest Request { get; init; } = null!;
        public WithdrawalStepViewModel Steps { get; init; } = null!;
        public FromToSelectionViewModel Branches { get; init; } = null!;
        public AgentSelectionViewModel Agents { get; init; } = null!;
        public DenomTableViewModel Denoms { get; init; } = null!;
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public Guid? EditingRequestId { get; set; }
        public DateOnly? FundSettlementValueDate { get; set; }
        public string? RtgsCode { get; set; }

    }
}
