namespace IdentityService.Core.Models
{
    public class ApplicationRoleViewModel
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationViewModel Application { get; set; }
        public string RoleId { get; set; }
        public RoleViewModel Role { get; set; }
    }
}
