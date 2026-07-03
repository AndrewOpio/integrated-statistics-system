namespace VMSService.Core.Models.BoardSessionDifferentDenoms
{
    public class BoardSessionDifferentDenomViewModel
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Amount { get; set; }
    }
}
