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

        // Populate to render an optional "Logistics" select inside the action form itself.
        // Left null/empty for callers that don't need it — no effect on any other module.
        public ICollection<VMSService.Data.Entities.Logistics>? LogisticsOptions { get; set; }
        public int? SelectedLogisticsId { get; set; }
    }
}
