using System.Security.Claims;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public class LdapUser
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string[] Groups { get; set; }
        public List<Claim> UserClaims { get; set; }
    }
}
