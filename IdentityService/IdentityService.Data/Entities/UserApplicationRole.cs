namespace IdentityService.Data.Entities
{
    public class UserApplicationRole
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }

        public int ApplicationRoleId { get; set; }
        public ApplicationRole ApplicationRole { get; set; }
    }
}
