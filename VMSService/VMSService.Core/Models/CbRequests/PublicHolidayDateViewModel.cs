namespace VMSService.Core.Models.CbRequests
{
    /// <summary>A single active public holiday, serialized to the page for the same-day/exceptional grace-window check (collection-date-rules.js).</summary>
    public class PublicHolidayDateViewModel
    {
        public string Date { get; set; } = string.Empty; // yyyy-MM-dd
        public string Name { get; set; } = string.Empty;
    }
}
