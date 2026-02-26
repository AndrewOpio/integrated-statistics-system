using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ExPrintMint: AuditableEntity
    {
        public int Id { get; set; }
        public string ExPrintMintReference { get; set; }
        public int CurrencyVendorId { get; set; }
        public CurrencyVendor CurrencyVendor { get; set; }
        public int DenominationId { get; set; }
        public Denomination Denomination { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public string BoxNumberPrefix { get; set; }
        public int? BoxNumberRangeStart { get; set; }
        public int? BoxNumberRangeEnd { get; set; }
        public string? SerialNumberPrefix { get; set; }
        public int? SerialNumberRangeStart { get; set; }
        public int? SerialNumberRangeEnd { get; set; }
        public string Attachment { get; set; }
        public int ApprovedCurrencyProcurementId { get; set; }
        public ApprovedCurrencyProcurement ApprovedCurrencyProcurement { get; set; }
        public int CurrentStatusId { get; set; }
        public StatusType CurrentStatus { get; set; }
        public bool IsDeleted { get; set; }
        public int NumberOfBoxes { get; set; }
    }
}
