using VMSService.Core.Models.AgentDetails;
using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.CbDepositRequests;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CbDepositRequest
{
    public class DepositRequestDetailsViewModel
    {
        public Guid Id { get; init; }
        public string? ReferenceNumber { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public int StatusTypeId { get; init; }

        // Dates
        public DateOnly DepositDate { get; init; }
        public DateOnly? RtgsDate { get; init; }
        public string? RtgsCode { get; init; }
        public bool RequiresSpecialApproval { get; set; }
        public string? SpecialApprovalReason { get; set; }

        // Users
        public string InitiatorName { get; init; } = string.Empty;

        // Branch
        public string CbBranchName { get; init; } = string.Empty;
        public string CbName { get; init; } = string.Empty;
        public string BouBranchName { get; init; } = string.Empty;
        public string BouBranchCode { get; init; } = string.Empty;
        public string CbBicCode { get; init; } = string.Empty;
        public string CbCode { get; init; } = string.Empty;
        public int BouBranchId { get; init; } = 0;
        // Collectors
        public IEnumerable<AgentDetailViewModel> DepositAgents { get; init; } = [];

        // Denominations (grouped by subcategory)
        public IEnumerable<DepositDenominationLineViewModel> FitNotes { get; init; } = [];
        public IEnumerable<DepositDenominationLineViewModel> UnfitNotes { get; init; } = [];
        public IEnumerable<DepositDenominationLineViewModel> NANotes { get; init; } = [];
        public IEnumerable<DepositDenominationLineViewModel> Coins { get; init; } = [];

        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; }
            = new List<WorkflowTransitionDto>();

        // Totals
        public decimal TotalNotesAmount { get; init; }
        public decimal TotalCoinsAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string TotalAmountInWords { get; init; } = string.Empty;

        // Nav Steps
        public DepositStepViewModel Steps { get; init; } = null!;

        // Action Permissions
        public bool CanEdit { get; init; }
        public bool CanSubmitToSupervisor { get; init; }
        public bool CanApprove { get; init; }
        public bool CanReturn { get; init; }
        public bool CanCancel { get; init; }

        public IEnumerable<DepositRequestStatusHistoryViewModel> StatusHistory { get; set; } = [];

        // Fees 
        public decimal ProcessingFee { get; set; }
        public decimal MinimumDepositPenaltyFee { get; set; }
        public decimal TotalFees { get; set; }
        public bool HasPenaltyViolations { get; set; }
        public string PenaltyDetails { get; set; } = string.Empty;

        // Panelists
        public string PanelAName { get; set; } = string.Empty;
        public string PanelBName { get; set; } = string.Empty;

        // All attachments for this deposit request
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }
}