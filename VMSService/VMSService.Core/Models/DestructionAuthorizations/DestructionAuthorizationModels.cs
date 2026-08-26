using VMSService.Core.Models.Attachments;
using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.DestructionAuthorizations
{
    public class UpdateDestructionAuthorizationDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int BranchId { get; set; }
        public DateTime Date { get; set; }
        public List<DestructionAuthorizationDenominationDto> Denominations { get; set; } = new();
    }

    public class CreateDestructionAuthorizationDto
    {
        public string ReferenceNumber { get; set; } = null!;
        public int BranchId { get; set; }
        public DateTime Date { get; set; }
        public List<DestructionAuthorizationDenominationDto> Denominations { get; set; } = new();
    }

    public class DestructionAuthorizationDenominationDto
    {
        public int DenominationId { get; set; }
        public string? NumberOfBags { get; set; }
        public decimal Amount { get; set; }
    }

    public class DestructionAuthorizationDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public IEnumerable<DestructionAuthorizationDenomination> Denominations { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public IEnumerable<DestructionAuthorizationStatusHistoryViewModel> StatusHistory { get; set; } = [];
        public IEnumerable<AttachmentViewModel> AllAttachments { get; set; } = [];
    }

    public class DestructionAuthorizationStatusHistoryViewModel
    {
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = string.Empty;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
