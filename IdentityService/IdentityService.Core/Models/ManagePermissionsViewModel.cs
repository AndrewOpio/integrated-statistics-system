namespace IdentityService.Core.Models
{
    public class ManagePermissionsViewModel
    {
        public int ApplicationRoleId { get; set; }
        public string RoleName { get; set; }
        public string ApplicationName { get; set; }
        public List<PermissionGroupViewModel> PermissionGroups { get; set; } = [];
    }
}
