using System.Security.Claims;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public interface IJwtTokenService
    {
        string GenerateToken(LdapUser user);
        ClaimsPrincipal ValidateToken(string token);
    }
}
