namespace VMSService.Core.Models.Bags
{
    public class BagDenominationViewModel
    {
        public int Id { get; set; }
        public int BagId { get; set; }
        public string? BagReferenceNumber { get; set; }
        public Guid DepositRequestDenominationId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;

        // Navigation properties - enriched from related entities
        public string? TransactionRefNumber { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public string? DenominationTypeName { get; set; }
        public int? Bundles { get; set; }
        public int? Boxes { get; set; }
        public decimal Amount { get; set; }
        public string Origin { get; set; } = "Cb";
        public int SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
    }
}