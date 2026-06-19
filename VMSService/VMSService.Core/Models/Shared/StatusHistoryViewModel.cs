using VMSService.Core.Models.Attachments;

namespace VMSService.Core.Models.Shared
{
    public class StatusHistoryViewModel
    {
        public string OldStatusName { get; set; } = string.Empty;
        public string NewStatusName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public IEnumerable<AttachmentViewModel> Attachments { get; set; } = [];
    }
}
