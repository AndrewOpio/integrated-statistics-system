namespace VMSService.Core.Models.StockMovement
{
    public class StockMovementDenominationViewModel
    {
        public int DenominationId { get; set; }
        public int? NumBagsBoxes { get; set; }
        public string? ContainerType { get; set; }
        public int? NumBundles { get; set; }
        public int? CategoryId { get; set; }
        public decimal SubTotalValue { get; set; }

        // Buffer/Operational per denomination
        public string? Categorization { get; set; } // "buffer" or "operational"
    }
}
