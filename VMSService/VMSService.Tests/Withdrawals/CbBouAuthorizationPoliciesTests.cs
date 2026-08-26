using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using VMSService.Core.Authorization;
using VMSService.Core.Static;
using VMSService.Web.Authorization;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 09: the <c>CbOnly</c>/<c>BouOnly</c> policies (<c>Program.cs</c>, wired through
/// <see cref="CbBouAuthorizationPolicies"/>) used to be built with <c>.RequireRole(...)</c>, which
/// checks <c>ClaimTypes.Role</c> — a claim type this app never populates. Real role claims arrive
/// on the "appRole" claim as "{AppAcronym}:{RoleName}" (the same bug ticket 08 fixed in
/// <c>CbOwnershipAuthorizationHandler</c>). As written, both policies would have rejected every
/// authenticated user, CB and BOU alike. These tests build the exact same policy registration
/// Program.cs uses — via <see cref="CbBouAuthorizationPolicies.AddCbBouPolicies"/>, not a copy —
/// against a real <see cref="IAuthorizationService"/>.
/// </summary>
public class CbBouAuthorizationPoliciesTests
{
    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder().AddCbBouPolicies();
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal UserWithRole(string? role) => new(new ClaimsIdentity(
        role is null
            ? []
            : [new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}")],
        authenticationType: role is null ? null : "TestAuth"));

    [Theory]
    [InlineData(RolePolicy.CbMaker)]
    [InlineData(RolePolicy.CbApprover)]
    public async Task CbOnly_Succeeds_For_Either_Cb_Role(string role)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(role), resource: null, RolePolicy.CbOnly);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(RolePolicy.BouChecker)]
    [InlineData(RolePolicy.PanelA)]
    [InlineData(RolePolicy.PanelB)]
    [InlineData(RolePolicy.RemittanceOfficer)]
    [InlineData(RolePolicy.BackOffice)]
    [InlineData(RolePolicy.CurrencyApprover)]
    public async Task CbOnly_Denies_Every_Bou_Role(string bouRole)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(bouRole), resource: null, RolePolicy.CbOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CbOnly_Denies_Unauthenticated()
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(null), resource: null, RolePolicy.CbOnly);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(RolePolicy.BouChecker)]
    [InlineData(RolePolicy.PanelA)]
    [InlineData(RolePolicy.PanelB)]
    [InlineData(RolePolicy.RemittanceOfficer)]
    [InlineData(RolePolicy.BackOffice)]
    [InlineData(RolePolicy.CurrencyApprover)]
    public async Task BouOnly_Succeeds_For_Every_Bou_Role(string bouRole)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(bouRole), resource: null, RolePolicy.BouOnly);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(RolePolicy.CbMaker)]
    [InlineData(RolePolicy.CbApprover)]
    public async Task BouOnly_Denies_Either_Cb_Role(string cbRole)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(cbRole), resource: null, RolePolicy.BouOnly);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task BouOnly_Denies_Unauthenticated()
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(UserWithRole(null), resource: null, RolePolicy.BouOnly);

        Assert.False(result.Succeeded);
    }
}
