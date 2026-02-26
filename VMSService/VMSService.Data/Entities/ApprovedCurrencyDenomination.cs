using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class ApprovedCurrencyDenomination: AuditableEntity
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public Denomination Denomination { get; set; }
        public int ApprovedCurrencyId { get; set; }
        public ApprovedCurrencyProcurement ApprovedCurrency { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Amount { get; set; } // = denomination_value * count
        public bool IsDeleted { get; set; }
        public int CurrencyVendorId { get; set; }   
        public CurrencyVendor CurrencyVendor { get; set; }
    }
}
