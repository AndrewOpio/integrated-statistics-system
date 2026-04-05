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
    public class WithdrawalRequestDetailsViewModel
    {
        public Guid Id { get; init; }
        public string? ReferenceNumber { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public int StatusId { get; init; }

        // ── Dates ─────────────────────────────────
        public DateOnly CollectionDate { get; init; }
        public DateOnly? RtgsDate { get; init; }
        public string? RtgsCode { get; init; }
        public bool RequiresSpecialApproval { get; init; }

        // ── Users ─────────────────────────────────
        public string InitiatorName { get; init; } = string.Empty;

        // ── Branch ────────────────────────────────
        public string CbBranchName { get; init; } = string.Empty;
        public string CbName { get; init; } = string.Empty;
        public string CbBranchLocation { get; init; } = string.Empty;
        public string CbBranchDistrinct { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;
        public string BouBranchCode { get; init; } = string.Empty;

        // ── Collectors ────────────────────────────
        public IEnumerable<AgentDetailViewModel> WithdrawAgents { get; init; } = [];

        // ── Denominations ─────────────────────────
        public IEnumerable<DenominationLineViewModel> Notes { get; init; } = [];
        public IEnumerable<DenominationLineViewModel> Coins { get; init; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; }
        = new List<WorkflowTransitionDto>();
        // ── Totals ────────────────────────────────
        public decimal TotalNotesAmount { get; init; }
        public decimal TotalCoinsAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string TotalAmountInWords { get; init; } = string.Empty;


        // ── Nav Steps ─────────────────────────────
        public WithdrawalStepViewModel Steps { get; init; } = null!;

        // ── Action Permissions ────────────────────
        public bool CanEdit { get; init; }
        public bool CanSubmitToSupervisor { get; init; }
        public bool CanApprove { get; init; }
        public bool CanReturn { get; init; }
        public bool CanCancel { get; init; }

        public IEnumerable<WithdrawalRequestStatusHistory> StatusHistory { get; set; } = [];
    }
}
