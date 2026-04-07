using IdentityService.Data.AuditColumns;

namespace IdentityService.Data.Entities
{
    public class CommercialBankUser : AuditableEntity
    {
       public int Id { get; set; }
       public string UserId { get; set; }
       public User User { get; set; }
       public int CommercialBankId { get; set; }
       public int? CommercialBankBranchId { get; set; }
       public bool IsDeleted { get; set; }
    }
}
