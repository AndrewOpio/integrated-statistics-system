namespace VMSService.Core.Models.StockMovement
{
    public class InternalStockMovementDenominationViewModel
    {
        public int DenominationId { get; set; }
        public int? NumBoxes { get; set; }
        public string? ContainerType { get; set; }
        public decimal SubTotalValue { get; set; }
        public List<SerialNumberRangeViewModel> SerialRanges { get; set; }
    }
}
