using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Logistics: AuditableEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        // Navigation property
        public virtual ICollection<LogisticsCategory> Categories { get; set; } = new List<LogisticsCategory>();
    }

    public class LogisticsCategory
    {
        public int Id { get; set; }

        public int LogisticsId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int SortOrder { get; set; }

        // Navigation properties
        public virtual Logistics Logistics { get; set; } = null!;
        public virtual ICollection<LogisticsDetail> Details { get; set; } = new List<LogisticsDetail>();
    }

    public class LogisticsDetail
    {
        public int Id { get; set; }

        public int LogisticsCategoryId { get; set; }

        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int SortOrder { get; set; }

        // Navigation property
        public virtual LogisticsCategory Category { get; set; } = null!;
    }
}
