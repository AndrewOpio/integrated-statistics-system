using IdentityService.Data.AuditColumns;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityService.Data.Entities
{
    [Table("ApplicationRoleBranches")]
    public class ApplicationRoleBranch : AuditableEntity
    {
        public int Id { get; set; }
        public int ApplicationRoleId { get; set; }
        public ApplicationRole ApplicationRole { get; set; } = null!;
        public int BranchId { get; set; }
    }
}
