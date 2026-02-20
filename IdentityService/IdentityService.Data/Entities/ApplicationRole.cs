using IdentityService.Data.AuditColumns;

namespace IdentityService.Data.Entities
{
    public class ApplicationRole : AuditableEntity
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public Application Application { get; set; }
        public string RoleId { get; set; }
        public Role Role { get; set; }
    }
}
