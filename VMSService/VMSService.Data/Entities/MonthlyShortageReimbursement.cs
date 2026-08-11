using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("MonthlyShortageReimbursements")]
    public class MonthlyShortageReimbursement : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int MonthlyShortageId { get; set; }
        public int BranchId { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public MonthlyShortage MonthlyShortage { get; set; } = null!;
        public ICollection<MonthlyShortageReimbursementDenomination> Denominations { get; set; } = [];
    }
}
