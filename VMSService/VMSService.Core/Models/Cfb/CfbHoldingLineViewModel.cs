namespace VMSService.Core.Models.Cfb
{
    /// <summary>One denomination row inside a box detail view — shows baseline, deviations, and actual.</summary>
    public class CfbHoldingLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;

        /// <summary>Active baseline amount in UGX (the allocation / expected position).</summary>
        public long BaselineAmount { get; init; }

        /// <summary>Sum of all AmountChange movements for this denomination in this box (signed).</summary>
        public long MovementTotal { get; init; }

        /// <summary>BaselineAmount + MovementTotal — what is actually in the box right now.</summary>
        public long ActualAmount { get; init; }

        /// <summary>Positive = surplus above baseline; negative = shortfall. Equal to MovementTotal.</summary>
        public long Deviation => ActualAmount - BaselineAmount;
    }
}