using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class VendorType : AuditableEntity
    {
       public int Id { get; set; }
       public string Name { get; set; }
       public string? Description { get; set; }
       public bool IsDeleted { get; set; }
    }
}
