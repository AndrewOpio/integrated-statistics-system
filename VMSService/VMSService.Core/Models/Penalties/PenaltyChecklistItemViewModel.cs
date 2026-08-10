namespace VMSService.Core.Models.Penalties
{
    public class PenaltyChecklistItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? PenaltyValue { get; set; }
        public bool IsChecked { get; set; }

        /// <summary>True if an active PenaltyWaiver covers this penalty for the request's branch and collection date.</summary>
        public bool IsWaived { get; set; }
        public DateOnly? WaiverEndDate { get; set; }
    }
}
