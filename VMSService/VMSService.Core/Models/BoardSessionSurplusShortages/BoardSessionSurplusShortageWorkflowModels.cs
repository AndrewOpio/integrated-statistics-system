using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Shared;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.BoardSessionSurplusShortages
{
    public class BoardSessionSurplusShortageIndexItemViewModel
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public string? SessionReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public bool IsSurplus { get; set; }
        public bool IsDifferentDenomination { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public string? CommercialBankName { get; set; }
        public string? BranchName { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class BoardSessionSurplusShortageDetailsViewModel
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public string? SessionReferenceNumber { get; set; }
        public decimal Amount { get; set; }
        public bool IsSurplus { get; set; }
        public bool IsDifferentDenomination { get; set; }
        public bool IsProcessedOnRtgs { get; set; }
        public string? RtgsCode { get; set; }
        public int CommercialBankId { get; set; }
        public string? CommercialBankName { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int CfbBoxId { get; set; }
        public string? CfbBoxName { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public IEnumerable<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public IEnumerable<StatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }
}
