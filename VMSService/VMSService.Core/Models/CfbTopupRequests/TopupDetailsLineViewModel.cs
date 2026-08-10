namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupDetailsLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;
        public int DenomValue { get; init; }
        public long CurrentBaseline { get; init; }
        public long IncrementAmount { get; init; }
        public long NewBaseline => CurrentBaseline + IncrementAmount;
        public string BoxName { get; init; } = string.Empty;
    }
}
