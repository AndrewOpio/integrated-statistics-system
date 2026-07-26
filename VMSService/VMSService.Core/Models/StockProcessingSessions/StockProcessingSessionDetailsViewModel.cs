using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.BoardSessionDifferentDenoms;
using VMSService.Core.Models.BoardSessionSurplusShortages;
using VMSService.Core.Models.StockProcessingResults;
using VMSService.Core.Models.StockProcessingSessionStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.StockProcessingSessions
{
    public class StockProcessingSessionDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int StockProcessingPlanId { get; set; }
        public int CommercialBankId { get; set; }
        public string? CommercialBankName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int? ProcessingBoardId { get; set; }
        public string? ProcessingBoardName { get; set; }
        public string? MachineName { get; set; }
        public string? CountingRoomName { get; set; }
        public string? SupervisorName { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public decimal TotalIssuedValue { get; set; }
        public bool IsManual { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<SessionBagDetailsInfo> Bags { get; set; } = [];
        public ICollection<StockProcessingResultViewModel> Results { get; set; } = [];
        public ICollection<BoardSessionDifferentDenomViewModel> DifferentDenoms { get; set; } = [];
        public ICollection<BoardSessionSurplusShortageViewModel> SurplusShortages { get; set; } = [];
        public ICollection<StockProcessingSessionStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
        public string BoardSupervisorUserId { get; set; } = string.Empty;
        public ICollection<SessionNoteExaminerViewModel> NoteExaminers { get; set; } = [];
        public List<NoteExaminerOptionViewModel> AvailableNoteExaminers { get; set; } = [];
    }

    public class NoteExaminerOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SessionBagDetailsInfo
    {
        public int BagId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? SubCategoryName { get; set; }
        public decimal DenominationValue { get; set; }
        public int TotalBundles { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
