namespace VMSService.Core.Models.BagMovements
{
    public class BagSelectionRowViewModel
    {
        public int BagId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string? VaultName { get; set; }
        public string? VaultSectionName { get; set; }
        public decimal TotalAmount { get; set; }
        public string? SubCategoryName { get; set; }
        public bool IsSelected { get; set; }
    }
}
