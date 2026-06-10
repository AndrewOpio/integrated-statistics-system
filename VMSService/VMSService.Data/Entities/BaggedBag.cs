using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BaggedBag : AuditableEntity
    {
        public int Id { get; set; }
        public int ParentBagId { get; set; }
        public Bag? ParentBag { get; set; }
        public int ChildBagId { get; set; }
        public Bag? ChildBag { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}