namespace VMSService.Core.Models.ExPrintMintStatusHistories
{
    public class ExPrintMintStatusHistoryViewModel
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}