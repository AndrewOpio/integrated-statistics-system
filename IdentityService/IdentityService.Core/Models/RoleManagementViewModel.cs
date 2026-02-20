using Microsoft.AspNetCore.Mvc.Rendering;

namespace IdentityService.Core.Models
{
    public class RoleManagementViewModel
    {
        public List<RoleViewModel> Roles { get; set; }
        
        public RoleViewModel Role { get; set; } //For creating new role
        public List<SelectListItem> SelectRoles { get; set; }
        public string SelectedRoleId { get; set; }
        public List<SelectListItem> SelectApplications { get; set; }
        public int? SelectedApplicationId { get; set; }
        public List<ApplicationRoleViewModel> ApplicationRoles { get; set; }
        
    }
}
