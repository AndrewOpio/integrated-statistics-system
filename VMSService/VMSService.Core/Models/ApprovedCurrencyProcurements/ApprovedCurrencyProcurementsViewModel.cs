using Microsoft.AspNetCore.Http;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.ApprovedCurrencyStatusHistories;
using VMSService.Core.Models.StatusTypes;
using VMSService.Core.Models.WorkflowTransitions;

namespace VMSService.Core.Models.ApprovedCurrencyProcurements
{
    public class ApprovedCurrencyProcurementsViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? BoardApprovalAttachment { get; set; }
        public int CurrentStatusId { get; set; }
        public StatusTypeViewModel? CurrentStatus { get; set; }
        public List<ApprovedCurrencyDenominationViewModel> ApprovedCurrencyDenominations { get; set; } = new();
        public IFormFile AttachmentFile { get; set; }
        public DateTime? CreatedDate { get; set; }
        public ICollection<WorkflowTransitionViewModel> AvailableTransitions { get; set; } = new List<WorkflowTransitionViewModel>();
        public ICollection<ApprovedCurrencyStatusHistoryViewModel> StatusHistory { get; set; } = new List<ApprovedCurrencyStatusHistoryViewModel>();
    }
}
