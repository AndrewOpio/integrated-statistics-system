using VMSService.Data.AuditColumns;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class Denomination : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int NumberOfPiecesInABox { get; set; }
        public int NumberOfPiecesInABundle { get; set; }
        public int DenominationTypeId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DenominationType? DenominationType { get; set; }
        public ICollection<StockLedgerDetail> StockLedgerDetails { get; set; }
    }
}
