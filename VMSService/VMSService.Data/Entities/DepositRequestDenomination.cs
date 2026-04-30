using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DepositRequestDenomination : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DepositRequestId { get; set; }
        public DepositRequest DepositRequest { get; set; } = null!;
        // Snapshot from denomination microservice
        public int DenominationId { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public int? Bundles { get; set; }   // for notes
        public int? Boxes { get; set; }     // for coins
        public decimal Amount { get; set; } // (Bundles ?? Boxes) × 1,000 × DenominationValue
        public string Origin { get; set; } = "Cb"; // "Cb" or "BOU"
        public int SubCategoryId { get; set; } // 1 - "FIT", 2 - "UNFIT", 3 - "N/A"

    }
}
