using WorkflowService.Client.Dto;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Models.CbDepositRequests;

namespace VMSService.Core.Models.CbDepositRequest
{
    public class CreateDepositViewModel
    {
        public DepositStepViewModel Steps { get; init; } = null!;
        public FromToSelectionViewModel Branches { get; init; } = null!;
        public AgentSelectionViewModel Agents { get; init; } = null!;
        public DepositDenomTableViewModel Denoms { get; init; } = null!;
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public Guid? EditingRequestId { get; set; }
        public DateOnly DepositDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }
    }
}
