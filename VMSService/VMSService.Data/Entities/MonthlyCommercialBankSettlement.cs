using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    [Table("MonthlyCommercialBankSettlements")]
    public class MonthlyCommercialBankSettlement : AuditableEntity
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public int CommercialBankId { get; set; }
        public DateTime Period { get; set; }
        public decimal TotalSurplus { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal NetPosition { get; set; }
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = "Draft";

        public ICollection<MonthlyCommercialBankSettlementStatusHistory>? StatusHistories { get; set; }
    }
}
