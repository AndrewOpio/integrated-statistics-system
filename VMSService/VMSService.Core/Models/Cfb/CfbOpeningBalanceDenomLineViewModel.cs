namespace VMSService.Core.Models.Cfb
{
    public class CfbOpeningBalanceDenomLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;
        public int DenomValue { get; init; }

        /// <summary>Non-null = an active baseline already exists. Show as read-only.</summary>
        public long? ExistingAmount { get; init; }
        public bool IsSet => ExistingAmount.HasValue;
    }
}
