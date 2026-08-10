namespace VMSService.Core.Models.Penalties
{
    public class PenaltyChecklistItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal? PenaltyValue { get; set; }
        public decimal? PenaltyPercent { get; set; }
        public decimal? PercentCutOff { get; set; }
        public decimal? PenaltyMultiplier { get; set; }
        public bool IsChecked { get; set; }

        /// <summary>Previously applied quantity, used to pre-fill the quantity input for multiplier-based penalties.</summary>
        public int? Quantity { get; set; }

        /// <summary>True for the fixed set of penalties applied automatically by the system — the modal
        /// should display these read-only rather than as a checkable option.</summary>
        public bool IsAutomatic { get; set; }

        /// <summary>True if an active PenaltyWaiver covers this penalty for the request's branch and collection date.</summary>
        public bool IsWaived { get; set; }
        public DateOnly? WaiverEndDate { get; set; }
    }
}
