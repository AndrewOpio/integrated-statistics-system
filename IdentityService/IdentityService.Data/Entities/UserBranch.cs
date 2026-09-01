using IdentityService.Data.AuditColumns;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityService.Data.Entities
{
    [Table("UserBranches")]
    public class UserBranch : AuditableEntity
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public int BranchId { get; set; }
    }
}
