namespace StockService.Core.Models.StockLedger
{
    public class CreateStockLedgerDto
    {
        public string ReferenceCode { get; set; }
        public int StockMovementId { get; set; }
        public bool IsIn { get; set; }
        public int CounterPartyTypeId { get; set; }
        public int CounterPartyId { get; set; }
        public int StoreId { get; set; }
        public string Notes { get; set; }
        public List<CreateStockLedgerDetailDto> StockLedgerDetails { get; set; } = new List<CreateStockLedgerDetailDto>();
    }

    public class CreateStockLedgerDetailDto
    {
        public int DenominationId { get; set; }
        public int CategoryId { get; set; }
        public int NumberOfPieces { get; set; }
        public int NumberOfBoxes { get; set; }
        public int? NumberOfBundles { get; set; }
        public decimal Value { get; set; } // Accept decimal from client
        public int? SubCategoryId { get; set; }
    }
}
