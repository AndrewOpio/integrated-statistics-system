using System.Security.Claims;
using VMSService.Core.Authorization;

namespace VMSService.Core.Extensions
{
    // Account-type checks (which side of the CB/BOU boundary), not role-workflow checks. Both
    // claims are issued together and are mutually exclusive by construction: AuthController only
    // adds a "cbId" claim when the account's IsStaff is false, and always adds "isStaff" either
    // way. See ticket 34.
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsBouStaff(this ClaimsPrincipal user) =>
            user.HasClaim(c => c.Type == "isStaff" && c.Value == "true");

        public static bool IsCbUser(this ClaimsPrincipal user) =>
            user.Claims.Any(c => c.Type == RolePolicy.CbIdClaim && !string.IsNullOrEmpty(c.Value));
    }
}
