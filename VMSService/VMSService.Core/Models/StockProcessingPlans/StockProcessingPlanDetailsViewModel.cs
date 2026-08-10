using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Bags;
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
}
