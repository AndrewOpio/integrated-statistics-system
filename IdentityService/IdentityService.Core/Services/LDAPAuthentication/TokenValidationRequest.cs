using System.ComponentModel.DataAnnotations;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class TokenValidationRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
