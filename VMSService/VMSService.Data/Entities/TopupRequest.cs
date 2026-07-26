using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class TopupRequest : AuditableEntity
    {
        public int Id { get; set; }
        public int BouBranchId { get; set; }
        public string? Justification { get; set; }

        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;

        public ICollection<TopupLine> Lines { get; set; } = new List<TopupLine>();
        public ICollection<TopupRequestStatusHistory> StatusHistory { get; set; } = new List<TopupRequestStatusHistory>();
    }
}
