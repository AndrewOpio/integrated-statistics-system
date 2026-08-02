using VMSService.Core.Models.Attachments;
using VMSService.Core.Models.Shared;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.BagMovements
{
    public class BagMovementDetailsViewModel
    {
        public int Id { get; set; }
        public List<BagSelectionRowViewModel> Bags { get; set; } = new();
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int NewVaultSectionId { get; set; }
        public string? NewVaultSectionName { get; set; }
        public string? NewVaultName { get; set; }
        public int? CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool CanEdit { get; set; }

        public IEnumerable<WorkflowTransitionDto> AvailableTransitions { get; set; } = [];
        public IEnumerable<StatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }
}
