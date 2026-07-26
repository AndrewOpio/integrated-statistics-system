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

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class WithdrawalRequestDetailsViewModel
    {
        public Guid Id { get; init; }
        public string? TransactionRefNumber { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public int StatusId { get; init; }
        public DateOnly CollectionDate { get; init; }
        public DateTime CreatedDate { get; init; }
        public DateOnly? FundSettlementValueDate { get; init; }
        public string? RtgsCode { get; init; }
        public bool RequiresSpecialApproval { get; set; }
        public string? ExceptionalApprovalReason { get; set; }
        public bool IsPostApprovalView { get; set; }
        //public string InitiatorName { get; init; } = string.Empty;

        public string CbCode { get; init; } = string.Empty;
        public string CbName { get; init; } = string.Empty;
        //public string CbBranchLocation { get; init; } = string.Empty;
        //public string CbBranchDistrinct { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;
        public string BouBranchCode { get; init; } = string.Empty;

        public IEnumerable<AgentDetailViewModel> WithdrawAgents { get; init; } = [];

        
        public IEnumerable<DenominationLineViewModel> Notes { get; init; } = [];
        public IEnumerable<DenominationLineViewModel> Coins { get; init; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
       
        public decimal TotalNotesAmount { get; init; }
        public decimal TotalCoinsAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public bool IsLumpSum { get; set; } = false;


        public WithdrawalStepViewModel Steps { get; init; } = null!;

        public bool CanEdit { get; init; }
        public bool CanEditRtgs { get; init; }
        public string RtgsVerificationStatus { get; init; } = "Pending";
        public bool RecallRtgsPending { get; init; }
        public string? RecallRtgsCode { get; init; }
        public DateOnly? RecallRtgsDate { get; init; }

        public string? ReturnRtgsCode { get; init; }
        public DateOnly? ReturnRtgsDate { get; init; }

        public string? CurrentStatusName { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public bool ShowExceptionalApprovalWarning { get; set; }
        public bool ShowSameDayMakerWarning { get; set; }
        public bool ShowSameDayReasonCapture { get; set; }
        public bool CanEditSameDayReason { get; set; }
        public IEnumerable<WithdrawalRequestStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }
}
