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
    }
}
