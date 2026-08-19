using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class MonthlyShortageReimbursementDenomination : AuditableEntity
    {
        public int Id { get; set; }
        public int MonthlyShortageReimbursementId { get; set; }
        public int DenominationId { get; set; }
        public string DenominationName { get; set; } = null!;
        public decimal DenominationValue { get; set; }
        public decimal Amount { get; set; }
        public int NumberOfPieces { get; set; }

        public MonthlyShortageReimbursement Reimbursement { get; set; } = null!;
    }
}
