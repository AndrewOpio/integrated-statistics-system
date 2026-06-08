using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class NewStockMovementlog : AuditableEntity
    {
        public int Id { get; set; }
        public int StockMovementId { get; set; }
        public long PackingListDetailId { get; set; }
        public int SerialNoteStartNumber { get; set; }
        public int SerialNoteEndNumber { get; set; }
        public int? AccountingStatusId { get; set; }
        public int? CaseFullStatusId { get; set; }

        public StockMovement StockMovement { get; set; }
        public PackingListDetails PackingListDetail { get; set; }
        public NewStockAccountingStatus AccountingStatus { get; set; }
        public NewCaseFullStatusTypes CaseFullStatus { get; set; }
    }
}
