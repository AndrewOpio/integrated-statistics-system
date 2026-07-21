using VMSService.Core.Models.Attachments;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.MonthlySurpluses
{
    public class CreateMonthlySurplusDto
    {
        public int BranchId { get; set; }
        public DateTime Period { get; set; }
        public decimal Amount { get; set; }
        public DateTime? EftDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateMonthlySurplusDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public DateTime Period { get; set; }
        public decimal Amount { get; set; }
        public DateTime? EftDate { get; set; }
        public string? Remarks { get; set; }
    }

    public class MonthlySurplusDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public decimal Amount { get; set; }
        public DateTime? EftDate { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public List<MonthlySurplusEodEntryViewModel> EodEntries { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public IEnumerable<MonthlySurplusStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> Attachments { get; set; } = [];
    }

    public class MonthlySurplusEodEntryViewModel
    {
        public DateTime SessionDate { get; set; }
        public bool IsSurplus { get; set; }
        public decimal Amount { get; set; }
        public string CommercialBankName { get; set; } = string.Empty;
        public string DenominationName { get; set; } = "—";
    }

    public class MonthlySurplusStatusHistoryViewModel
    {
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = string.Empty;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public IEnumerable<AttachmentViewModel> Attachments { get; set; } = [];
    }
}
