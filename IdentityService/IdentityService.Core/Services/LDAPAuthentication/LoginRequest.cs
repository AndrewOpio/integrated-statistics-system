using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
