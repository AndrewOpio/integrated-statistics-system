using VMSService.Core.Static;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Verifies the BOU side (Back Office / Panel A / Panel B / Currency Approver) of the rebuilt
/// WithdrawalRequest transitions (ticket 01) against the live local WorkflowService/IdentityService
/// databases.
/// </summary>
public class BouSideWorkflowTransitionsTests
{
    private static readonly int BackOfficeRoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.BackOffice);
    private static readonly int PanelARoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.PanelA);
    private static readonly int PanelBRoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.PanelB);
    private static readonly int CurrencyApproverRoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.CurrencyApprover);

    [Fact]
    public void BackOffice_Can_SubmitVerification_From_PendingRTGSConfirmation()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingWithdrawalConfirmation);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.Submit_Verification
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingVerification);
    }

    [Fact]
    public void BackOffice_Can_SubmitVerificationExceptional_From_PendingRTGSConfirmation()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingWithdrawalConfirmation);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.Submit_Verification_Exceptional
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingExceptionalApproval);
    }

    [Fact]
    public void BackOffice_Can_ReportRTGSConfirmationFailed_From_PendingRTGSConfirmation()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingWithdrawalConfirmation);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.RtgsConfirmationFailed
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.VerificationFailed);
    }

    [Fact]
    public void BackOffice_Can_ReturnForRevision_From_RTGSConfirmationFailed()
    {
        // Origin 3 of 3 (ADR-0001) — new row added by ticket 01, didn't exist before the rebuild.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.VerificationFailed);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ReturnForRevision
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.ReturnedForRevision);
    }

    [Fact]
    public void BackOffice_Not_CurrencyApprover_Can_ApproveExceptional_From_PendingExceptionalApproval()
    {
        // This is the exact live-config bug the rebuild fixes: ApproveExceptional/RejectExceptional
        // were wrongly pointed at Currency Approver, sourced from a stray "VerifyExceptional" step.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingExceptionalApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ApproveExceptional
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingVerification);
        Assert.DoesNotContain(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ApproveExceptional && t.RequiredRole == CurrencyApproverRoleId);
    }

    [Fact]
    public void BackOffice_Not_CurrencyApprover_Can_RejectExceptional_From_PendingExceptionalApproval()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingExceptionalApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.RejectExceptional
            && t.RequiredRole == BackOfficeRoleId
            && t.ToStatus == WithdrawalStatusNames.RejectExceptionalApproval);
        Assert.DoesNotContain(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.RejectExceptional && t.RequiredRole == CurrencyApproverRoleId);
    }

    [Fact]
    public void No_Transition_Routes_Through_VerifyExceptional_Anymore()
    {
        // VerifyExceptional / "Pending Exceptional Verification" was a mistake, not intended
        // design — ApproveExceptional/RejectExceptional now fire directly off Pending Exceptional
        // Approval with no intermediate step.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingExceptionalApproval);

        Assert.DoesNotContain(transitions, t => t.ActionName == WithdrawalStatusActionNames.VerifyExceptional);
        Assert.Equal(0, WorkflowConfigReader.CountTransitionsReferencingStatus("Pending Exceptional Verification"));
    }

    [Fact]
    public void PanelB_Can_ReturnForRevision_From_PendingVerification()
    {
        // Origin 2 of 3 (ADR-0001).
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingVerification);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ReturnForRevision
            && t.RequiredRole == PanelBRoleId
            && t.ToStatus == WithdrawalStatusNames.ReturnedForRevision);
    }

    [Fact]
    public void PanelB_Can_SubmitForApproval_From_PendingVerification()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingVerification);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.SubmitForApproval
            && t.RequiredRole == PanelBRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingApproval);
    }

    [Fact]
    public void PanelA_Can_Approve_From_PendingApproval()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.Approve
            && t.RequiredRole == PanelARoleId
            && t.ToStatus == WithdrawalStatusNames.Approved);
    }

    [Fact]
    public void PanelA_Can_Reject_From_PendingApproval()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.Reject
            && t.RequiredRole == PanelARoleId
            && t.ToStatus == WithdrawalStatusNames.Rejected);
    }

    [Fact]
    public void CurrencyApprover_Can_ApproveRecall_From_RecallVerified()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.RecallVerified);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ApproveRecall
            && t.RequiredRole == CurrencyApproverRoleId
            && t.ToStatus == WithdrawalStatusNames.RecallApproved);
    }

    [Fact]
    public void CurrencyApprover_Can_RejectRecall_From_RecallVerified()
    {
        // RejectRecall's ToStatus is a placeholder — ADR-0001's code-side logic overrides it with
        // a direct mutation back to whatever status the recall was requested from — but
        // WorkflowService still needs a matching row for the action to be recognized at all.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.RecallVerified);

        Assert.Contains(transitions, t =>
            t.ActionName == "RejectRecall" && t.RequiredRole == CurrencyApproverRoleId);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Cancel Complete")]
    [InlineData("Pending Exceptional Verification")]
    public void RemovedStatuses_Are_Not_Referenced_By_Any_Transition(string removedStatusName)
    {
        // There is no Cancel in this domain — Recall is the CB's only withdrawal mechanism.
        Assert.Equal(0, WorkflowConfigReader.CountTransitionsReferencingStatus(removedStatusName));
    }
}
