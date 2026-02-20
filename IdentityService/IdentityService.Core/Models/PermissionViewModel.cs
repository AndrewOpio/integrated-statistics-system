namespace IdentityService.Core.Models
{
    public class PermissionViewModel
    {
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public bool IsSelected { get; set; }
    }
}
