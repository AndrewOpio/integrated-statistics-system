using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class LogisticsDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int LogisticsCategoryId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Quantity { get; set; }
        public decimal Frequency { get; set; } = 1;
        public decimal TotalCost { get; set; }
        [MaxLength(500)]
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
        public virtual LogisticsCategory Category { get; set; } = null!;
    }
}
