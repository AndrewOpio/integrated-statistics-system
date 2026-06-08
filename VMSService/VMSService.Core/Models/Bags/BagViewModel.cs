namespace VMSService.Core.Models.Bags
{
    public class BagViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsDeleted { get; set; }
        public int BranchId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}