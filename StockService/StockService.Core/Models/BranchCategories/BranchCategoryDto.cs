using StockService.Core.Models.Branches;
using StockService.Core.Models.Categories;

namespace StockService.Core.Models.BranchCategories
{
    public class BranchCategoryDto
    {
        public int Id { get; set; }
        public BranchDto? Branch { get; set; }
        public CategoryDto? Category { get; set; }
        public bool IsDeleted { get; set; }
    }
}
