namespace VMSService.Core.Models.Bags
{
    public class NestedBagViewModel
    {
        public int BaggedBagId { get; set; }
        public int ChildBagId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int TotalBundles { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
    }
}
