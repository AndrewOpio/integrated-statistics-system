using StockService.Web.Models.Branches;
using StockService.Web.Models.Categories;

namespace StockService.Web.Models.BranchCategories
{
    public class BranchCategoryDto
    {
        public int Id { get; set; }
        public BranchDto? Branch { get; set; }
        public CategoryDto? Category { get; set; }
        public bool IsDeleted { get; set; }
    }
}
