namespace VMSService.Core.Models.BoardSessionSurplusShortages
{
    public class BoardSessionSurplusShortageViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public bool IsSurplus { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int CommercialBankId { get; set; }
        public string? CommercialBankName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
