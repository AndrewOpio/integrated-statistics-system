using VMSService.Core.Models.Attachments;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.Shared
{
    public class WorkflowSectionsViewModel
    {
        public int EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
        public IEnumerable<StatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public string ControllerName { get; set; } = string.Empty;
        public object? AdditionalValidation { get; set; }
    }
}