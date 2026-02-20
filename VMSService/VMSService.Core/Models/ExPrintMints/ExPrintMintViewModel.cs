using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.CurrencyVendors;
using VMSService.Core.Models.Denominations;

namespace VMSService.Core.Models.ExPrintMints
{
    public class ExPrintMintViewModel
    {
        public int Id { get; set; }
        public string ExPrintMintReference { get; set; }
        public int CurrencyVendorId { get; set; }
        public CurrencyVendorViewModel CurrencyVendor { get; set; }
        public int DenominationId { get; set; }
        public DenominationViewModel Denomination { get; set; }
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
        public ApprovedCurrencyProcurementsViewModel ApprovedCurrencyProcurement { get; set; }
    }
}