namespace IdentityService.Core.Models
{
    public class UserRoleManagementViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        //public List<UserApplicationRoleViewModel> UserApplicationRoles { get; set; }
        public Dictionary<string, List<ApplicationRoleSelectViewModel>> ApplicationRolesByApplications { get; set; }
    }
}
