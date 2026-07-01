using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class WithdrawalRequestDenomination : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WithdrawalRequestId { get; set; }
        public WithdrawalRequest WithdrawalRequest { get; set; } = null!;

        // Snapshot from denomination microservice
        public int DenominationId { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public int? Bundles { get; set; }   // for notes
        public int? Boxes { get; set; }     // for coins
        public decimal Amount { get; set; } // (Bundles ?? Boxes) × 1,000 × DenominationValue
        public string Origin { get; set; } = "Cb";
        public string BundleType { get; set; } = "NA";

        public ICollection<WithdrawalRequestDenominationBox> DispatchedBoxes { get; set; }
            = new List<WithdrawalRequestDenominationBox>();

    }
}
