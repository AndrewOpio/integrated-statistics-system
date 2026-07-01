using VMSService.Core.Models.Attachments;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.MonthlyCommercialBankSettlements
{
    public class MonthlyCommercialBankSettlementDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int CommercialBankId { get; set; }
        public string CommercialBankName { get; set; } = null!;
        public DateTime Period { get; set; }
        public decimal TotalSurplus { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal NetPosition { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = null!;
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedDate { get; set; }

        public List<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public List<MonthlyCommercialBankSettlementStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> Attachments { get; set; } = [];
    }

    public class MonthlyCommercialBankSettlementStatusHistoryViewModel
    {
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = null!;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = null!;
        public string ChangedBy { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public List<AttachmentViewModel> Attachments { get; set; } = [];
    }
}
