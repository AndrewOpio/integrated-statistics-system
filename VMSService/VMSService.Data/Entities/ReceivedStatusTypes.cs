using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ReceivedStatusTypes : AuditableEntity
    {
        public int Id { get; set; }
        public string StatusName { get; set; }
        public string StatusDescription { get; set; }
        public ICollection<ReceivedStock> ReceivedStock { get; set; }
    }
}
