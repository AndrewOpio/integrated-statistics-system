using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Models 
{ 
    public class RoleViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [Display(Name = "Creator")]
        public string CreatedBy { get; set; } = "system";

        [Display(Name = "Modifier")]
        public string ModifiedBy { get; set; } = "system";
        public List<ApplicationViewModel> Applications { get; set; } = new List<ApplicationViewModel>();
    }
}
