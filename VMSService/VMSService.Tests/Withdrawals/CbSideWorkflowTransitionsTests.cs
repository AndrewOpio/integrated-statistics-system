using VMSService.Core.Static;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Verifies the CB Maker / CB Approver side of the rebuilt WithdrawalRequest transitions (ticket
/// 01) matches what VMSService's constants (ticket 02) actually send, against the live local
/// WorkflowService/IdentityService databases.
/// </summary>
public class CbSideWorkflowTransitionsTests
{
    private static readonly int CbMakerRoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.CbMaker);
    private static readonly int CbApproverRoleId = WorkflowConfigReader.GetRoleId(SystemPolicy.Roles.CbApprover);

    [Fact]
    public void CbMaker_Can_SubmitForInternalApproval_From_Draft()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.Draft);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.SubmitForInternalApproval
            && t.RequiredRole == CbMakerRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingInternalApproval);
    }

    [Fact]
    public void CbApprover_Can_ReturnForRevision_From_PendingInternalApproval()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingInternalApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ReturnForRevision
            && t.RequiredRole == CbApproverRoleId
            && t.ToStatus == WithdrawalStatusNames.ReturnedForRevision);
    }

    [Fact]
    public void CbMaker_Can_ResubmitForApproval_From_ReturnedForRevision()
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.ReturnedForRevision);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ResubmitForApproval
            && t.RequiredRole == CbMakerRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingInternalApproval);
    }

    [Fact]
    public void CbApprover_Can_ApproveToRTGSCode_From_PendingInternalApproval()
    {
        // Regression check for the ApproveToRTGSVefification typo: VMSService's constant and
        // WorkflowService's live row must be the exact same corrected string, or the transition
        // silently stops matching.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingInternalApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ApproveToRTGSVerification
            && t.RequiredRole == CbApproverRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingRTGSVerification);
    }

    [Fact]
    public void CbApprover_Can_SubmitExceptionalWithdrawalRequest_From_PendingInternalApproval()
    {
        // Regression check for the ExceptionalWithdrawaRequest typo.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.PendingInternalApproval);

        Assert.Contains(transitions, t =>
            t.ActionName == WithdrawalStatusActionNames.ExceptionalWithdrawalRequest
            && t.RequiredRole == CbApproverRoleId
            && t.ToStatus == WithdrawalStatusNames.PendingWithdrawalConfirmation);
    }

    public static IEnumerable<object[]> RecallEligibleStatuses =>
    [
        [WithdrawalStatusNames.PendingRTGSVerification],
        [WithdrawalStatusNames.PendingWithdrawalConfirmation],
        [WithdrawalStatusNames.PendingExceptionalApproval],
        [WithdrawalStatusNames.PendingVerification],
        [WithdrawalStatusNames.PendingApproval],
    ];

    [Theory]
    [MemberData(nameof(RecallEligibleStatuses))]
    public void RequestRecall_Is_Available_To_Both_CbMaker_And_CbApprover(string fromStatus)
    {
        var transitions = WorkflowConfigReader.GetTransitionsFrom(fromStatus);
        var recallTransitions = transitions.Where(t => t.ActionName == WithdrawalStatusActionNames.RequestRecall).ToList();

        Assert.Contains(recallTransitions, t => t.RequiredRole == CbMakerRoleId && t.ToStatus == WithdrawalStatusNames.RecallRequested);
        Assert.Contains(recallTransitions, t => t.RequiredRole == CbApproverRoleId && t.ToStatus == WithdrawalStatusNames.RecallRequested);
    }

    [Fact]
    public void RequestRecall_Is_Not_Available_From_RTGSConfirmationFailed()
    {
        // Confirmed decision (workflow-transitions.md): RTGS Confirmation Failed already has its
        // own dedicated way out via Back Office's Return — Recall would be a redundant second path.
        var transitions = WorkflowConfigReader.GetTransitionsFrom(WithdrawalStatusNames.VerificationFailed);

        Assert.DoesNotContain(transitions, t => t.ActionName == WithdrawalStatusActionNames.RequestRecall);
    }
}
