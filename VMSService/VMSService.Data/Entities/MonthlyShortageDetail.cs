using System.ComponentModel.DataAnnotations.Schema;

namespace VMSService.Data.Entities
{
    [Table("MonthlyShortageDetails")]
    public class MonthlyShortageDetail
    {
        public int Id { get; set; }
        public int MonthlyShortageId { get; set; }
        public int BranchId { get; set; }
        public decimal TotalSurplus { get; set; }
        public decimal TotalShortage { get; set; }
        public decimal NetShortage { get; set; }

        public virtual MonthlyShortage MonthlyShortage { get; set; } = null!;
    }
}
