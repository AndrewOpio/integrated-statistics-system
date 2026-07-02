using System.ComponentModel.DataAnnotations.Schema;

namespace VMSService.Data.Entities
{
    [Table("EodDiscrepancyReportEntries")]
    public class EodDiscrepancyReportEntry
    {
        public int Id { get; set; }
        public int EodDiscrepancyReportId { get; set; }
        public int BoardSessionSurplusShortageId { get; set; }

        public virtual EodDiscrepancyReport EodDiscrepancyReport { get; set; } = null!;
        public virtual BoardSessionSurplusShortage BoardSessionSurplusShortage { get; set; } = null!;
    }
}
