using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
            var role = context.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;

            // Bank users can access any request
            if (RolePolicy.BouRoles.Contains(role))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Company users can only access requests belonging to their company
            var cbIdClaim = context.User.Claims
                .FirstOrDefault(c => c.Type == RolePolicy.CbIdClaim)?.Value;

            if (!string.IsNullOrEmpty(cbIdClaim)
                && Guid.TryParse(cbIdClaim, out var claimCompanyId)
                && resource.CbId == claimCompanyId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
