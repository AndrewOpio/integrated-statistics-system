using StockService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DepositFee : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int FeeTypeId { get; set; }
        public FeeType FeeType { get; set; }
        public Guid DepositRequestId { get; set; }
        public DepositRequest DepositRequest { get; set; }
        public decimal Amount { get; set; }
        public bool IsWaived { get; set; } = false;
        public string? WaiverReason { get; set; }
        public DateTime? WaivedDate { get; set; }
        public string? WaivedBy { get; set; }
        public bool IsDeleted { get; set; } = false;

    }
}
