using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Models
{
	public class UserViewModel
	{
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
		public string? MiddleName { get; set; }
		public string LastName { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
		public DateTime? DisableDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsLoggedIn { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsStaff { get; set; }
        public DateTime CreatedDate { get ; set ; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [Display(Name = "Creator")]
        public string CreatedBy { get; set; } = "system";

        [Display(Name = "Modifier")]
        public string ModifiedBy { get; set; } = "system";
    }
}
