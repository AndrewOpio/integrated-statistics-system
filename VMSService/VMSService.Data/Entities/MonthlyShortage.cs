using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("MonthlyShortages")]
    public class MonthlyShortage : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime Period { get; set; }
        public decimal TotalSurplus { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal NetShortage { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<MonthlyShortageDetail>? Details { get; set; }
        public ICollection<MonthlyShortageStatusHistory>? StatusHistories { get; set; }
    }
}
