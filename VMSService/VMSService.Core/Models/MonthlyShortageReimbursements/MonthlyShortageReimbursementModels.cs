using VMSService.Core.Models.Attachments;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.MonthlyShortageReimbursements
{
    public class MonthlyShortageReimbursementIndexViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public string ShortageReference { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";
    }

    public class ReimbursementIndexPageViewModel
    {
        public List<MonthlyShortageReimbursementIndexViewModel> Reimbursements { get; set; } = [];
    }

    public class CreateReimbursementViewModel
    {
        public List<ShortageOptionViewModel> ShortageOptions { get; set; } = [];
        public List<DenomOptionViewModel> DenominationOptions { get; set; } = [];
    }

    public class ShortageOptionViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime Period { get; set; }
        public List<BranchOptionViewModel> Branches { get; set; } = [];
    }

    public class BranchOptionViewModel
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
    }

    public class DenomOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
    }

    public class CreateReimbursementRequest
    {
        public int MonthlyShortageId { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
        public List<ReimbursementDenominationRequest> Denominations { get; set; } = [];
    }

    public class ReimbursementDenominationRequest
    {
        public int DenominationId { get; set; }
        public decimal Amount { get; set; }
        public int NumberOfPieces { get; set; }
    }

    public class ReimbursementDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public string ShortageReference { get; set; } = null!;
        public DateTime ShortagePeriod { get; set; }
        public string BranchName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = null!;
        public List<ReimbursementDenominationDetailViewModel> Denominations { get; set; } = [];
        public List<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public List<ReimbursementStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> Attachments { get; set; } = [];
    }

    public class ReimbursementDenominationDetailViewModel
    {
        public string DenominationName { get; set; } = null!;
        public decimal DenominationValue { get; set; }
        public decimal Amount { get; set; }
        public int NumberOfPieces { get; set; }
    }

    public class ReimbursementStatusHistoryViewModel
    {
        public string OldStatusName { get; set; } = null!;
        public string NewStatusName { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public List<AttachmentViewModel> Attachments { get; set; } = [];
    }
}
