using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupDetailsViewModel
    {
        public int Id { get; init; }
        public string BouBranchName { get; init; } = string.Empty;
        public string? Justification { get; init; }
        public string CurrentStatusName { get; init; } = string.Empty;
        public int CurrentStatusId { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; }
        public long TotalAmount { get; init; }

        public List<TopupDetailsLineViewModel> Lines { get; init; } = [];
        public List<TopupStatusHistoryViewModel> StatusHistory { get; init; } = [];
        public List<WorkflowTransitionDto> AvailableTransitions { get; init; } = [];
    }
}
