using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class Denomination : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int NumberOfPiecesInABox { get; set; }
        public int DenominationTypeId { get; set; }
        //public DenominationType? DenominationType { get; set; } = null;
        public bool IsDeleted { get; set; }
        //public List<StockLedgerDetail> StockLedgerDetail { get; set; } = new List<StockLedgerDetail>();
    }
}
