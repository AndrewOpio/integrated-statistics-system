using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Authorization;
using VMSService.Web.Extensions;

namespace VMSService.Web.Authorization
{
    public static class CbBouAuthorizationPolicies
    {
        // RequireRole checks ClaimTypes.Role, which this app never populates — roles arrive on
        // the "appRole" claim as "{AppAcronym}:{RoleName}" (same bug found and fixed in
        // CbOwnershipAuthorizationHandler, ticket 08). RequireAssertion against GetAppRoleNames()
        // reads the claim this app actually issues.
        public static AuthorizationBuilder AddCbBouPolicies(this AuthorizationBuilder builder) =>
            builder
                .AddPolicy(RolePolicy.CbOnly, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx => ctx.User.GetAppRoleNames().Any(RolePolicy.CbRoles.Contains)))
                .AddPolicy(RolePolicy.BouOnly, policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(ctx => ctx.User.GetAppRoleNames().Any(RolePolicy.BouRoles.Contains)));
    }
}
