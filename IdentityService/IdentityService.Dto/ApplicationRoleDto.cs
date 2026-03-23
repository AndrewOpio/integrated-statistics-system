namespace IdentityService.Dto
{
    public class ApplicationRoleDto
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationDto Application { get; set; }
        public string RoleId { get; set; }
        public RoleDto Role { get; set; }
    }
}
