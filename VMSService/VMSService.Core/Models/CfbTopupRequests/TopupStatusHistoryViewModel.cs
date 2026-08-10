namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupStatusHistoryViewModel
    {
        public string OldStatusName { get; init; } = string.Empty;
        public string NewStatusName { get; init; } = string.Empty;
        public string ChangedBy { get; init; } = string.Empty;
        public DateTime ChangedAt { get; init; }
        public string? Remarks { get; init; }
    }
}
