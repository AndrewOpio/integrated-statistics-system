using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Extensions;
using VMSService.Core.Models.CbRequests;

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
            // Account-type check, not a role check — any BOU staff member can access any bank's
            // request, regardless of which specific role they hold. See ticket 34.
            if (context.User.IsBouStaff())
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
