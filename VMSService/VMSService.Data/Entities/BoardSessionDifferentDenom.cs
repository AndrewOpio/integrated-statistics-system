namespace VMSService.Data.Entities
{
    public class BoardSessionDifferentDenom
    {
        public int Id { get; set; }
        public int BoardSessionSurplusShortageId { get; set; }
        public int DenominationId { get; set; }
        public decimal Amount { get; set; }

        public virtual BoardSessionSurplusShortage? BoardSessionSurplusShortage { get; set; }
    }
}
