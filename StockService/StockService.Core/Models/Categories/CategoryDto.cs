namespace StockService.Core.Models.Categories
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Classification { get; set; }
        public bool IsDeleted { get; set; }
    }
}
