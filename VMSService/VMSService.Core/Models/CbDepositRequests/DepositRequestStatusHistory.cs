namespace VMSService.Core.Models.CbDepositRequest
{
    public class DepositRequestStatusHistoryViewModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DepositRequestId { get; set; }
        public DepositRequestDetailsViewModel DepositRequest { get; set; } = null!;
        public int OldStatusId { get; set; }
        public string? OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string? NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
