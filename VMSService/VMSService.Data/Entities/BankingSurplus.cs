using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("BankingSurpluses")]
    public class BankingSurplus : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int BranchId { get; set; }
        public DateTime Period { get; set; }
        public decimal Amount { get; set; }
        public DateTime? DepositDate { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<BankingSurplusStatusHistory>? StatusHistories { get; set; }
    }
}
