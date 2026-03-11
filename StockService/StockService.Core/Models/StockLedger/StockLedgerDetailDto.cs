using StockService.Core.Models.Denominations;

namespace StockService.Core.Models.StockLedger
{
    public class StockLedgerDetailDto
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public DenominationDto? Denomination { get; set; }
        public int NumberOfPieces { get; set; }
        public int NumberOfBoxes { get; set; }
        public int? NumberOfBundles { get; set; }
        public decimal Value { get; set; }
    }
}
