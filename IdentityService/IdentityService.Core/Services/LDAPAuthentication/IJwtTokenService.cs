using System.Security.Claims;

namespace IdentityService.Core.Services.LDAPAuthentication
{
    public interface IJwtTokenService
    {
        string GenerateToken(LdapUser user);
        ClaimsPrincipal ValidateToken(string token);

        /// <summary>
        /// Re-issues a still-valid token with a fresh expiry, carrying the original
        /// claims across. Used to extend a session without re-authenticating.
        /// Fails if the token is invalid, already expired, or if the absolute
        /// session cap (JwtSettings.MaxSessionHours) has been reached.
        /// </summary>
        TokenRenewalResult RenewToken(string token);
    }
}
