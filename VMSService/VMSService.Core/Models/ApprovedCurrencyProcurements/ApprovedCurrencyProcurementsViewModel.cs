using Microsoft.AspNetCore.Http;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.ApprovedCurrencyProcurements
{
    public class ApprovedCurrencyProcurementsViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? BoardApprovalAttachment { get; set; }
        public int CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }
        public List<ApprovedCurrencyDenominationViewModel> ApprovedCurrencyDenominations { get; set; } = new();
        public IFormFile AttachmentFile { get; set; }
        public DateTime? CreatedDate { get; set; }
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public ICollection<ApprovedCurrencyStatusHistoryViewModel> StatusHistory { get; set; } = new List<ApprovedCurrencyStatusHistoryViewModel>();
    }
}
