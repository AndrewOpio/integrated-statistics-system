using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ExPrintMintDetail : AuditableEntity
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public ExPrintMint ExPrintMint { get; set; }
        public int DenominationId { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public int NumberOfBoxes { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<PackingListRange>? PackingListRanges { get; set; }
    }
}
