using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Authorization;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 09: "a CB Approver can currently POST to Create/Edit/SaveDraft/Delete; only the Details
/// page hides the buttons" — <c>CbWithdrawRequestsController</c> gates its whole surface behind
/// one "Details" permission shared by both CB roles, so nothing server-side stopped an Approver
/// from mutating a request directly. Reuses the controller-building harness from
/// <see cref="CbWithdrawRequestsControllerOwnershipTests"/> (ticket 08) since it already mocks
/// everything these same four actions need — only the role on the claims principal changes here.
///
/// The guard (<c>IsCbMaker</c>) allowlists CbMaker specifically, which also rejects CbApprover and
/// every BOU role. BOU access to these four actions was never a deliberate design choice — see
/// <c>IsCbUser</c>/<c>IsCbMaker</c> on the controller and <see cref="CbWithdrawRequestsControllerOwnershipTests.NonPrintActionCases"/>.
/// </summary>
public class CbWithdrawRequestsControllerMakerCheckerTests
{
    private const int BankId = 1;

    private static ClaimsPrincipal CbUser(string role) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.Name, "cb-user"),
        new Claim(ClaimTypes.NameIdentifier, "cb-user-id"),
        new Claim(RolePolicy.CbIdClaim, BankId.ToString()),
        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{role}"),
    ]));

    public static IEnumerable<object[]> ActionCases()
    {
        yield return new object[] { "Create", WithdrawalStatusNames.Draft,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Create(CancellationToken.None)) };

        yield return new object[] { "Edit", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Edit(r.Id, CancellationToken.None)) };

        yield return new object[] { "SaveDraft", WithdrawalStatusNames.Draft,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.SaveDraft(
                    CbWithdrawRequestsControllerOwnershipTests.BuildValidSaveDraftVm(r), CancellationToken.None)) };

        yield return new object[] { "Delete", WithdrawalStatusNames.Approved,
            (Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>>)
                ((c, r) => c.Delete(r.Id, CancellationToken.None)) };
    }

    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Cb_Approver_Is_Denied_On_Every_Mutating_Action(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = CbWithdrawRequestsControllerOwnershipTests.BuildController(
            BankId, status, CbUser(RolePolicy.CbApprover));

        var result = await invoke(controller, request);

        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Cb_Maker_Is_Unaffected_On_Every_Mutating_Action(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = CbWithdrawRequestsControllerOwnershipTests.BuildController(
            BankId, status, CbUser(RolePolicy.CbMaker));

        var result = await invoke(controller, request);

        Assert.IsNotType<ForbidResult>(result);
    }

    // Corrected from an earlier "unaffected" version of this test: BOU access to
    // Create/Edit/SaveDraft/Delete was never actually a deliberate design choice — it was a side
    // effect of CbOwnershipAuthorizationHandler's blanket "any BOU role bypasses ownership" rule,
    // which ticket 08 wired onto every action on this controller without checking whether each
    // one specifically needed it. Only PrintRequest does (linked from the BOU-side Details page).
    // BOU editing/deleting a CB's draft, or updating its RTGS code, is exactly the kind of gap an
    // audit would (rightly) flag. See IsCbUser() on the controller.
    [Theory]
    [MemberData(nameof(ActionCases))]
    public async Task Bou_Role_Is_Denied_On_Every_Mutating_Action(
        string actionName, string status, Func<CbWithdrawRequestsController, WithdrawalRequest, Task<IActionResult>> invoke)
    {
        var (controller, request) = CbWithdrawRequestsControllerOwnershipTests.BuildController(
            BankId, status, CbWithdrawRequestsControllerOwnershipTests.BouUser(RolePolicy.PanelA));

        var result = await invoke(controller, request);

        Assert.IsType<ForbidResult>(result);
    }
}
