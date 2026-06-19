using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.Shared
{
    public class WorkflowActionsViewModel
    {
        public int EntityId { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public IEnumerable<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public bool IsValid { get; set; } = true;
        public string? AdditionalValidationMessage { get; set; }
    }
}
