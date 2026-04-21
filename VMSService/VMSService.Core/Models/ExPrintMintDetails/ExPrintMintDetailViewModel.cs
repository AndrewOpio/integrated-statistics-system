namespace VMSService.Core.Models.ExPrintMintDetails
{
    public class ExPrintMintDetailViewModel
    {
        public int Id { get; set; }
        public int ExPrintMintId { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public int NumberOfPieces { get; set; }
        public decimal Value { get; set; }
        public int NumberOfBoxes { get; set; }
        public bool IsDeleted { get; set; }
    }
}
