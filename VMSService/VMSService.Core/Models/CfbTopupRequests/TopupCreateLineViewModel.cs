namespace VMSService.Core.Models.CfbTopupRequests
{
    public class TopupCreateLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;
        public int DenomValue { get; init; }
        public long CurrentBaseline { get; init; }

        /// <summary>When editing, the bundle/box quantity already saved on this line — null if not yet added.</summary>
        public int? ExistingQuantity { get; init; }
    }
}
