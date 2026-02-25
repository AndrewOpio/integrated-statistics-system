using StockService.Core.Models.Denominations;

namespace StockService.Core.Models.StockLedger
{
    public class StockLedgerDetailDto
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public DenominationDto? Denomination { get; set; }
        public int Count { get; set; }
        public int Values { get; set; }
    }
}
