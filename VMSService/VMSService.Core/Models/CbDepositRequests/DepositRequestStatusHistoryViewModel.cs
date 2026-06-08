using VMSService.Core.Models.Attachments;

namespace VMSService.Core.Models.CbDepositRequests
{
    public class DepositRequestStatusHistoryViewModel
    {
        public Guid Id { get; set; }
        public Guid DepositRequestId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = string.Empty;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? AttachmentIds { get; set; }
        public ICollection<AttachmentViewModel> Attachments { get; set; } = new List<AttachmentViewModel>();
    }
}
