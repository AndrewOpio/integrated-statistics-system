using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.WithdrawalRequestDenominations;
using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.WithdrawalRequests
{
    public class BouWithdrawalRequestDetailsViewModel
    {
        public Guid Id { get; init; }
        public string? TransactionRefNumber { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public int StatusId { get; init; }
        public DateOnly CollectionDate { get; init; }
        public DateTime CreatedDate { get; init; }
        public DateOnly? RtgsDate { get; init; }
        public string? RtgsCode { get; init; }
        public bool RequiresSpecialApproval { get; set; }
        public string? SpecialApprovalReason { get; set; }
        public bool IsPostApprovalView { get; set; }

        public string CbCode { get; init; } = string.Empty;
        public string CbName { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;
        public string BouBranchCode { get; init; } = string.Empty;

        public decimal TotalNotesAmount { get; init; }
        public decimal TotalCoinsAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public bool IsLumpSum { get; set; } = false;
        public string? CurrentStatusName { get; set; }
        public bool CanEditDispatch { get; set; }
        public string RtgsVerificationStatus { get; init; } = "Pending";
        public DateOnly? RtgsVerifiedDate { get; init; }
        public bool CanVerifyRtgs { get; init; }
        public bool RecallRtgsPending { get; init; }
        public string? RecallRtgsCode { get; init; }
        public DateOnly? RecallRtgsDate { get; init; }
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
        public DenomTableViewModel Denoms { get; init; } = null!;
        public WithdrawalStepViewModel Steps { get; init; } = null!;
        public IEnumerable<AgentDetailViewModel> WithdrawAgents { get; init; } = [];

        public IEnumerable<DenominationLineViewModel> Notes { get; init; } = [];
        public IEnumerable<DenominationLineViewModel> Coins { get; init; } = [];
        public IEnumerable<WithdrawalRequestDenomination> CbDenominations { get; init; } = [];
        public List<BouDenominationEditRowViewModel> EditableBankRows { get; set; } = new();
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public List<BouDispatchCardViewModel> DispatchedNewBanknotes { get; set; } = new();
        public List<BouDispatchSimpleViewModel> DispatchedReissueBanknotes { get; set; } = new();
        public List<BouDispatchSimpleViewModel> DispatchedCoins { get; set; } = new();
    }
}
