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

        // Password fields for non-staff users
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string? PhoneNumber { get; set; }

        // Helper properties for phone number input
        [Display(Name = "Country Code")]
        public string? CountryCode { get; set; } = "+256";

        [Display(Name = "Phone")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Please enter a valid 9-digit phone number.")]
        public string? PhoneNumberLocal { get; set; }
    }
}
