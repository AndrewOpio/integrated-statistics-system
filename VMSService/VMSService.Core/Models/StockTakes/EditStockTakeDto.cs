namespace VMSService.Core.Models.StockTakes
{
    public class EditStockTakeDto
    {
        public int BranchId { get; set; }
        public DateTime CountDate { get; set; }
        public string? Remarks { get; set; }
        public List<StockTakeLineDto> Lines { get; set; } = new();
    }
}
