using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowService.Client.Dto;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    public class CreateWithdrawViewModel
    {
        public WithdrawalStepViewModel Steps { get; init; } = null!;
        public FromToSelectionViewModel Branches { get; init; } = null!;
        public AgentSelectionViewModel Agents { get; init; } = null!;
        public DenomTableViewModel Denoms { get; init; } = null!;
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public Guid? EditingRequestId { get; set; }
        public DateOnly CollectionDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }
    }
}
