using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkflowService.Core.Models.WorkflowTransitions
{
    public class WorkflowTransitionViewModel
    {
        public int Id { get; set; }
        public int EntityTypeId { get; set; }
        public string? EntityTypeName { get; set; }
        public int FromStatusId { get; set; }
        public string? FromStatusName { get; set; }
        public int ToStatusId { get; set; }
        public string? ToStatusName { get; set; }
        public string ActionName { get; set; }
        public int RequiredRole { get; set; }
        public string? RequiredRoleName { get; set; }
        public bool RequiresComments { get; set; }
        public string DisplayLabel { get; set; }
        public string ButtonClass { get; set; }
        public bool IsActive { get; set; }

        // Dropdown lists
        public List<SelectListItem> EntityTypesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> FromStatusesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ToStatusesList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> RequiredRoleList { get; set; } = new List<SelectListItem>();
    }
}