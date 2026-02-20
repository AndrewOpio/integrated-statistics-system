using VMSService.Core.Models.ApprovedCurrencyProcurements;
using VMSService.Core.Models.Denominations;

namespace VMSService.Core.Models.ApprovedCurrencyDenominations
{
    public class ApprovedCurrencyDenominationViewModel
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public DenominationViewModel Denomination { get; set; }
        public int ApprovedCurrencyId { get; set; }
        public ApprovedCurrencyProcurementsViewModel ApprovedCurrency { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; } // = denomination_value * count
    }
}
