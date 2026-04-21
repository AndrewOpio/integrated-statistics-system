using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ReceivedStockDetails : AuditableEntity
    {
        public int Id { get; set; }
        public int ReceivedStockId { get; set; }
        public int PackingListRangeId { get; set; }
        public int ReceivedStartCaseNumber { get; set; }
        public int ReceivedEndCaseNumber { get; set; }
        public bool IsDeleted { get; set; }

        public ReceivedStock ReceivedStock { get; set; }
        public PackingListRange PackingListRange { get; set; }
    }
}
