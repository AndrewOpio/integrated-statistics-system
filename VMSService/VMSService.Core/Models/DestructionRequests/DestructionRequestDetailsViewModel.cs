using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.DestructionRequests
{
    public class DestructionRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string AuthorizationReferenceNumber { get; set; } = string.Empty;
        public int? DestructionAuthorizationId { get; set; }
        public string DestructionTypeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string VerificationOfficer { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public IEnumerable<DestructionRequestDenomination> Denominations { get; set; } = [];
        public IEnumerable<DestructionRequestMember> Members { get; set; } = [];
        public IEnumerable<DestructionRequestMember> Witnesses { get; set; } = [];

        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public IEnumerable<DestructionRequestStatusHistoryViewModel> StatusHistory { get; set; } = [];
    }

    public class DestructionRequestStatusHistoryViewModel
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
