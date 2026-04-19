using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class PackingListRange : AuditableEntity
    {
        public int Id { get; set; }
        public int ExpectedStockDetailId { get; set; }
        public string CaseNumberPrefix { get; set; }
        public int StartCaseNumber { get; set; }
        public int EndCaseNumber { get; set; }
        public string SNPrefix { get; set; }
        public int SNRangeStart { get; set; }
        public int SNRangeEnd { get; set; }
        public int ExpectedStockId { get; set; }

        public ExPrintMint? ExpectedStock { get; set; }
        public ExPrintMintDetail? ExpectedStockDetail { get; set; }
        public ICollection<PackingListDetails>? PackingListDetails { get; set; }
        public ICollection<ReceivedStockDetails>? ReceivedStockDetails { get; set; }
    }
}
