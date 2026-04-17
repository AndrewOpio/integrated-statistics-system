using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class NewStockAccountingStatus : AuditableEntity
    {
        public int Id { get; set; }
        public string AccountingStatusName { get; set; }
        public string AccountingStatusDescription { get; set; }
        public ICollection<NewStockMovementlog> NewStockMovementLogs { get; set; }
    }
}
