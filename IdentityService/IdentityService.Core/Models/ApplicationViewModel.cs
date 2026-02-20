namespace IdentityService.Core.Models
{
    public class ApplicationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Acronym { get; set; }
        public string Status { get; set; } = "Active";
        public string? Url { get; set; }
    }
}
