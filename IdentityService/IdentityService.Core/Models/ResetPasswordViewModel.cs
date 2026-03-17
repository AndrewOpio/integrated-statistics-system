using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        public string UserName { get; set; }

        public string DisplayName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}