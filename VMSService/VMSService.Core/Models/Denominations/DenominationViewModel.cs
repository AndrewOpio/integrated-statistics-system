using VMSService.Core.Models.DenominationTypes;

namespace VMSService.Core.Models.Denominations
{
    public class DenominationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
        public int DenominationTypeId { get; set; }
        public DenominationTypeViewModel? DenominationType { get; set; }
    }
}