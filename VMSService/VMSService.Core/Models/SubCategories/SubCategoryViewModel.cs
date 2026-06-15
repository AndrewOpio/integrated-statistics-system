using Microsoft.AspNetCore.Mvc.Rendering;

namespace VMSService.Core.Models.SubCategories
{
    public class SubCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsDeleted { get; set; } = false;
        
        // Dropdown list for categories
        public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
    }
}