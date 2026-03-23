using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using WorkflowService.Core.Models.EntityTypes;

namespace WorkflowService.Core.Models.StatusTypes
{
    public class StatusTypeViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Application")]
        public int ApplicationId { get; set; }

        [Display(Name = "Entity Type/Category")]
        public int EntityTypeId { get; set; }
        public EntityTypeViewModel? EntityType { get; set; }

        [Display(Name = "Application")]
        public string? ApplicationName { get; set; }

        // Dropdown lists
        public List<SelectListItem> Applications { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EntityTypes { get; set; } = new List<SelectListItem>();
    }
}