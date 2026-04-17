using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class NewCaseFullStatusTypes : AuditableEntity
    {
        public int Id { get; set; }
        public string StatusName { get; set; }
        public string StatusDescription { get; set; }
        public ICollection<NewStockMovementlog> NewStockMovementLogs { get; set; }
    }
}
