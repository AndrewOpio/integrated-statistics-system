using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Static;

namespace VMSService.Core.Authorization
{

    public class CbOwnershipAuthorizationHandler
        : AuthorizationHandler<CbOwnershipRequirement, WithdrawalRequestAuthContext>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CbOwnershipRequirement requirement,
            WithdrawalRequestAuthContext resource)
        {
            // Role claims arrive as "{AppAcronym}:{RoleName}" on the "appRole" claim type —
            // matches the extraction pattern used everywhere else in the app (e.g.
            // CbWithdrawRequestsController.Details). ClaimTypes.Role is never populated here.
            var roleNames = context.User.Claims
                .Where(c => c.Type == SystemPolicy.ClaimType.AppRole
                    && c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:"))
                .Select(c => c.Value.Split(":")[1]);

            // Bank users can access any request
            if (roleNames.Any(RolePolicy.BouRoles.Contains))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // CB users can only access requests belonging to their own bank
            var cbIdClaim = context.User.Claims
                .FirstOrDefault(c => c.Type == RolePolicy.CbIdClaim)?.Value;

            if (!string.IsNullOrEmpty(cbIdClaim)
                && int.TryParse(cbIdClaim, out var claimCbId)
                && resource.CbId == claimCbId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
