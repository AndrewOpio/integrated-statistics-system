using ConfigurationsService.Client;
using VMSService.Core.Models.Categories;

namespace VMSService.Core.Models.BranchCategories
{
    public class BranchCategoryViewModel
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public BranchDto? Branch { get; set; }
        public int CategoryId { get; set; }
        public CategoryViewModel? Category { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}