namespace VMSService.Core.Models.Penalties
{
    public class AppliedPenaltyViewModel
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public int PenaltyId { get; set; }
        public string PenaltyName { get; set; } = string.Empty;
        public decimal ChargeValue { get; set; }
        public int? Quantity { get; set; }
        public bool IsSettledOnRtgs { get; set; }
        public string? RtgsCode { get; set; }
        public string? RtgsSettlingMessage { get; set; }
        public string AppliedBy { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public bool ToBeWaived { get; set; }
        public string? WaiverReason { get; set; }
        public string? WaivedBy { get; set; }
        public DateTime? WaivedDate { get; set; }
    }
}
