using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CurrencyVendor : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Country { get; set; }
        public int VendorTypeId { get; set; }
        public VendorType VendorType { get; set; }
        public bool IsDeleted { get; set; }
    }
}
