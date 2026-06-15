using VMSService.Core.Models.CbRequests;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CbDepositRequests
{
    public class EditDepositViewModel
    {
        public Guid RequestId { get; set; }
        public DepositStepViewModel Steps { get; init; } = null!;
        public FromToSelectionViewModel Branches { get; init; } = null!;
        public AgentSelectionViewModel Agents { get; init; } = null!;
        public DepositDenomTableViewModel Denoms { get; init; } = null!;
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public DateOnly DepositDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }
        public bool RequiresSpecialApproval { get; set; }
        public string? SpecialApprovalReason { get; set; }
        public string TransactionRefNumber { get; set; } = string.Empty;
    }

    public class ExistingDenominationViewModel
    {
        public int DenominationId { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public int? Bundles { get; set; }
        public int? Boxes { get; set; }
        public decimal Amount { get; set; }
        public int SubCategoryId { get; set; }
    }
}