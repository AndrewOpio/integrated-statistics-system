using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbRequests;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 08 fixed the original Guid/int mismatch that made the CB same-bank check always fail.
/// Ticket 34 replaced the BOU bypass's role-list check (fragile — see the "BOU Checker"/"Bou
/// Checker" casing bug found auditing the real role catalog) with the "isStaff" claim: any BOU
/// staff member bypasses ownership, regardless of which specific role they hold. These tests
/// exercise the handler directly against the same claim shapes the real JWT produces.
/// </summary>
public class CbOwnershipAuthorizationHandlerTests
{
    private static ClaimsPrincipal CbUser(int cbId) => new(new ClaimsIdentity(
    [
        new Claim(RolePolicy.CbIdClaim, cbId.ToString()),
        new Claim("isStaff", "false"),
    ]));

    private static ClaimsPrincipal BouUser() => new(new ClaimsIdentity(
    [
        new Claim("isStaff", "true"),
    ]));

    private static async Task<bool> SucceedsAsync(ClaimsPrincipal user, int resourceCbId)
    {
        var handler = new CbOwnershipAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            [CbOwnershipRequirement.Instance], user, new WithdrawalRequestAuthContext { CbId = resourceCbId });

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task CbUser_Against_Own_Banks_Request_Succeeds()
    {
        // Regression case for the Guid/int mismatch: a real "2" cbId claim never parsed as a Guid,
        // so even this same-bank case used to fail.
        Assert.True(await SucceedsAsync(CbUser(cbId: 2), resourceCbId: 2));
    }

    [Fact]
    public async Task CbUser_Against_Other_Banks_Request_Is_Denied()
    {
        Assert.False(await SucceedsAsync(CbUser(cbId: 1), resourceCbId: 2));
    }

    [Fact]
    public async Task BouStaff_Succeeds_Against_Any_Banks_Request_Regardless_Of_Role()
    {
        // No role claim at all — proves the bypass is genuinely account-type-based (isStaff), not
        // dependent on holding any particular role name.
        Assert.True(await SucceedsAsync(BouUser(), resourceCbId: 999));
    }

    [Fact]
    public async Task Holding_A_Bou_Role_Name_Without_IsStaff_Is_Not_Enough()
    {
        // Regression guard for the switch away from role-list matching: a role name from
        // RolePolicy.BouRoles alone (no "isStaff" claim) must not bypass ownership.
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("appRole", $"CAS:{RolePolicy.PanelA}"),
        ]));

        Assert.False(await SucceedsAsync(user, resourceCbId: 999));
    }

    [Fact]
    public async Task User_With_Neither_IsStaff_Nor_A_CbId_Claim_Is_Denied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(await SucceedsAsync(user, resourceCbId: 1));
    }
}
