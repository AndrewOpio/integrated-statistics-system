namespace IdentityService.Core.Models
{
    public class UserApplicationRoleViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public UserViewModel User { get; set; }

        public int ApplicationRoleId { get; set; }
        public ApplicationRoleViewModel ApplicationRole { get; set; }
    }
}
