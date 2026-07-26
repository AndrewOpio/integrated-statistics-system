namespace VMSService.Core.Models.Penalties
{
    public class PenaltyChecklistItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? PenaltyValue { get; set; }
        public bool IsChecked { get; set; }
    }
}
