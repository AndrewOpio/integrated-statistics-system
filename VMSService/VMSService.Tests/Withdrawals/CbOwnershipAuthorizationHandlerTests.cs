using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Static;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 08: <c>CbOwnershipAuthorizationHandler</c> had two independent bugs that made it
/// impossible to wire up anywhere: (1) <c>WithdrawalRequestAuthContext.CbId</c> was <c>Guid</c>
/// while every actual <c>cbId</c> in the system (claim, entity column,
/// <c>IConfigurationsServiceClient</c>) is <c>int</c> — a real cbId claim like "2" would fail
/// <c>Guid.TryParse</c> and deny even a legitimate same-bank CB user; and (2) the BOU bypass read
/// <c>ClaimTypes.Role</c>, a claim type this app never populates — real role claims arrive on
/// "appRole" as "{AppAcronym}:{RoleName}" (see <c>CbWithdrawRequestsController.Details</c>), so no
/// BOU user could ever have satisfied it. These tests exercise the handler directly against the
/// same claim shapes the real JWT produces.
/// </summary>
public class CbOwnershipAuthorizationHandlerTests
{
    private static ClaimsPrincipal CbUser(int cbId, string role = RolePolicy.CbMaker) => new(new ClaimsIdentity(
    [
        new Claim(RolePolicy.CbIdClaim, cbId.ToString()),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
    ]));

    private static ClaimsPrincipal BouUser(string role) => new(new ClaimsIdentity(
    [
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
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

    [Theory]
    [InlineData(RolePolicy.BouChecker)]
    [InlineData(RolePolicy.PanelA)]
    [InlineData(RolePolicy.PanelB)]
    [InlineData(RolePolicy.RemittanceOfficer)]
    [InlineData(RolePolicy.BackOffice)]
    [InlineData(RolePolicy.CurrencyApprover)]
    public async Task BouUser_Of_Any_Role_Succeeds_Against_Any_Banks_Request(string bouRole)
    {
        // Regression case for the ClaimTypes.Role bug: none of these roles ever arrived on
        // ClaimTypes.Role, so the "bank users bypass this" branch was previously dead code.
        Assert.True(await SucceedsAsync(BouUser(bouRole), resourceCbId: 999));
    }

    [Fact]
    public async Task User_With_Neither_A_BouRole_Nor_A_CbId_Claim_Is_Denied()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.False(await SucceedsAsync(user, resourceCbId: 1));
    }
}
