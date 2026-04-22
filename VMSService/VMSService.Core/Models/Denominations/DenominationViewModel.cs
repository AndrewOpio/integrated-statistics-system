using VMSService.Core.Models.DenominationTypes;
using VMSService.Data.Entities;

namespace VMSService.Core.Models.Denominations
{
    public class DenominationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int NumberOfPiecesInABox { get; set; }
        public int NumberOfPiecesInABundle { get; set; }
        public int DenominationTypeId { get; set; }
        public DenominationTypeViewModel? DenominationType { get; set; }
        public int CurrencyVendorId { get; set; }
        public CurrencyVendor? CurrencyVendor { get; set; }
    }
}