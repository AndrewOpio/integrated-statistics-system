using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WorkflowService.Core.Models.EntityTypes
{
    public class EntityTypeViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Display(Name = "Application")]
        [Required(ErrorMessage = "Application is required")]
        public int ApplicationId { get; set; }
        public string? ApplicationName { get; set; }

        // Dropdown list for applications
        public List<SelectListItem> ApplicationsList { get; set; } = new List<SelectListItem>();
    }
}
