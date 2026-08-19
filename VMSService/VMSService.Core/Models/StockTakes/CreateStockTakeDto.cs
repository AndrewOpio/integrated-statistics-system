namespace VMSService.Core.Models.StockTakes
{
    public class CreateStockTakeDto
    {
        public int BranchId { get; set; }
        public DateTime CountDate { get; set; }
        public string? Remarks { get; set; }
        public int? AmendsStockTakeId { get; set; }
        public List<StockTakeLineDto> Lines { get; set; } = new();
    }

    public class StockTakeLineDto
    {
        public int CategoryId { get; set; }
        public int VaultSectionId { get; set; }
        public int DenominationId { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
    }
}
