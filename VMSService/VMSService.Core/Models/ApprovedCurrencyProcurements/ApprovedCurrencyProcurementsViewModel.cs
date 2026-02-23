using Microsoft.AspNetCore.Http;
using VMSService.Core.Models.ApprovedCurrencyDenominations;
using VMSService.Core.Models.StatusTypes;

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
    }
}
