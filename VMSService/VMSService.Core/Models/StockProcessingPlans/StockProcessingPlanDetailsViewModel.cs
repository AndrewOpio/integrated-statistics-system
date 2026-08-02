using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.MIandFits;
using VMSService.Core.Models.Shared;
using VMSService.Core.Models.StockProcessingPlanStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.StockProcessingPlans
{
    public class StockProcessingPlanDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public List<CountingRoomWithBagsViewModel> CountingRooms { get; set; } = new();
        public List<ProcessingSessionViewModel> Sessions { get; set; } = new();
        public List<SessionResultSummaryViewModel> ResultSummaries { get; set; } = new();
        public List<MIandFitViewModel> MIandFits { get; set; } = new();
        public ICollection<StockProcessingPlanStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }

    public class CountingRoomWithBagsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BoardSupervisorName { get; set; }
        public List<ProcessingBoardWithBagsViewModel> Boards { get; set; } = new();
    }

    public class ProcessingBoardWithBagsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public List<BagViewModel> Bags { get; set; } = new();
    }

    public class ProcessingSessionViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? BoardName { get; set; }
        public string? MachineName { get; set; }
        public string? CommercialBankName { get; set; }
        public decimal DenominationValue { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public List<SessionBagInfo> Bags { get; set; } = [];
    }

    public class SessionBagInfo
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? SubCategoryName { get; set; }
        public decimal DenominationValue { get; set; }
        public int TotalBundles { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SessionResultSummaryViewModel
    {
        public int SessionId { get; set; }
        public string SessionReferenceNumber { get; set; } = string.Empty;
        public decimal DenominationValue { get; set; }
        public decimal TotalIssuedValue { get; set; }
        public decimal ReissueValue { get; set; }
        public decimal CanceledValue { get; set; }
        public decimal FitShredValue { get; set; }
        public decimal UnProcessedValue { get; set; }
    }
}
