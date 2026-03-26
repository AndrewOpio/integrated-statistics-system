using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ApprovedCurrencyProcurement : AuditableEntity
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string BoardApprovalAttachment { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
