namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupListRowViewModel
    {
        public int Id { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public string CreatedBy { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; }
        public long TotalAmount { get; init; }
    }
}
