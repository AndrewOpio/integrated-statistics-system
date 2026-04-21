using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class PackingListDetails : AuditableEntity
    {
        public long Id { get; set; }
        public int PackingListRangeId { get; set; }
        public int CaseNumber { get; set; }
        public int SerialNoteStartNumber { get; set; }
        public int SerialNoteEndNumber { get; set; }

        public PackingListRange PackingListRange { get; set; }
        public ICollection<NewStockMovementlog> NewStockMovementLogs { get; set; }
    }
}
