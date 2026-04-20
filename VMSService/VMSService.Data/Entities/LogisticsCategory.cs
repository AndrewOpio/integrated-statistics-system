using System.Collections.Generic;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class LogisticsCategory : AuditableEntity
    {
        public int Id { get; set; }
        public int LogisticsId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public virtual Logistics Logistics { get; set; } = null!;
        public virtual ICollection<LogisticsDetail> Details { get; set; } = new List<LogisticsDetail>();
    }
}
