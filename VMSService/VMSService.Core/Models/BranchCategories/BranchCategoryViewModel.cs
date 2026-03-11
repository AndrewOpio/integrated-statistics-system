using VMSService.Core.Models.Categories;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Models.BranchCategories
{
    public class BranchCategoryViewModel
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public Branch? Branch { get; set; }
        public int CategoryId { get; set; }
        public CategoryViewModel? Category { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}