using VMSService.Core.Models.Attachments;

namespace VMSService.Core.Models.StockProcessingSessionStatusHistories
{
    public class StockProcessingSessionStatusHistoryViewModel
    {
        public int Id { get; set; }
        public int StockProcessingSessionId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public string? Attachments { get; set; } // Comma Separated Values of AttachmentIds
        public ICollection<AttachmentViewModel> AttachmentFiles { get; set; } = new List<AttachmentViewModel>();
    }
}
