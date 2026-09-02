using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Services.Withdrawals;
using VMSService.Core.Static;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Ticket 36: the BOU stepper collapses the withdrawal workflow onto a fixed 5-milestone backbone
/// instead of rendering one pill per StatusHistory row, so a heavily-returned request still renders
/// a fixed-size stepper. See WithdrawalMilestoneMap and .scratch/withdrawals/issues/36-*.md.
/// </summary>
public class WithdrawalHistoryTrailBuilderTests
{
    private static WithdrawalRequestStatusHistoryViewModel Entry(string oldStatus, string newStatus, DateTime changedAt)
        => new() { OldStatusName = oldStatus, NewStatusName = newStatus, ChangedAt = changedAt, ChangedBy = "tester" };

    [Fact]
    public void Fresh_Request_At_Pending_Internal_Approval_Is_On_First_Milestone_Only()
    {
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, DateTime.UtcNow),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.PendingInternalApproval);

        Assert.Equal(WithdrawalMilestoneMap.Backbone.Length, trail.Milestones.Count);
        Assert.True(trail.Milestones[0].IsReached);
        Assert.True(trail.Milestones[0].IsCurrent);
        Assert.All(trail.Milestones.Skip(1), m => Assert.False(m.IsReached));
        Assert.Null(trail.Exit);
        Assert.Equal(0, trail.ReturnCount);
    }

    [Fact]
    public void Request_Returned_Many_Times_Still_Reports_Fixed_Five_Milestones_With_A_Count()
    {
        var t = DateTime.UtcNow;
        var history = new List<WithdrawalRequestStatusHistoryViewModel>
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
        };
        // Simulate 6 CB-side return/resubmit cycles.
        for (int i = 0; i < 6; i++)
        {
            history.Add(Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.ReturnedForRevision, t.AddMinutes(i * 2 + 1)));
            history.Add(Entry(WithdrawalStatusNames.ReturnedForRevision, WithdrawalStatusNames.PendingInternalApproval, t.AddMinutes(i * 2 + 2)));
        }

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.PendingInternalApproval);

        Assert.Equal(5, trail.Milestones.Count);
        Assert.Equal(6, trail.ReturnCount);
        Assert.Null(trail.Exit);
    }

    [Fact]
    public void Request_At_Pending_Verification_Reached_Through_Third_Milestone()
    {
        var t = DateTime.UtcNow;
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
            Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.PendingRTGSVerification, t.AddMinutes(1)),
            Entry(WithdrawalStatusNames.PendingRTGSVerification, WithdrawalStatusNames.PendingWithdrawalConfirmation, t.AddMinutes(2)),
            Entry(WithdrawalStatusNames.PendingWithdrawalConfirmation, WithdrawalStatusNames.PendingVerification, t.AddMinutes(3)),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.PendingVerification);

        Assert.True(trail.Milestones[0].IsReached);
        Assert.True(trail.Milestones[1].IsReached);
        Assert.True(trail.Milestones[2].IsReached);
        Assert.True(trail.Milestones[2].IsCurrent);
        Assert.False(trail.Milestones[3].IsReached);
        Assert.False(trail.Milestones[4].IsReached);
        Assert.Null(trail.Exit);
    }

    [Fact]
    public void Exceptional_Path_Reaches_Verification_Milestone_Same_As_Normal_Path()
    {
        var t = DateTime.UtcNow;
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
            Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.PendingWithdrawalConfirmation, t.AddMinutes(1)),
            Entry(WithdrawalStatusNames.PendingWithdrawalConfirmation, WithdrawalStatusNames.PendingExceptionalApproval, t.AddMinutes(2)),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.PendingExceptionalApproval);

        Assert.True(trail.Milestones[2].IsReached);
        Assert.True(trail.Milestones[2].IsCurrent);
        Assert.Equal(WithdrawalMilestoneMap.Verification, trail.Milestones[2].Name);
    }

    [Fact]
    public void Rejected_Request_Shows_Reject_Exit_Label_Not_A_Sixth_Milestone()
    {
        var t = DateTime.UtcNow;
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
            Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.PendingRTGSVerification, t.AddMinutes(1)),
            Entry(WithdrawalStatusNames.PendingRTGSVerification, WithdrawalStatusNames.PendingWithdrawalConfirmation, t.AddMinutes(2)),
            Entry(WithdrawalStatusNames.PendingWithdrawalConfirmation, WithdrawalStatusNames.PendingVerification, t.AddMinutes(3)),
            Entry(WithdrawalStatusNames.PendingVerification, WithdrawalStatusNames.PendingApproval, t.AddMinutes(4)),
            Entry(WithdrawalStatusNames.PendingApproval, WithdrawalStatusNames.Rejected, t.AddMinutes(5)),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.Rejected);

        Assert.Equal("Rejected", trail.Exit?.Label);
        Assert.Equal(WithdrawalStatusNames.Rejected, trail.Exit?.StatusName);
        // Reached through Approval (milestone index 3) before exiting.
        Assert.True(trail.Milestones[3].IsReached);
        Assert.False(trail.Milestones[3].IsCurrent);
        Assert.False(trail.Milestones[4].IsReached);
        // The view renders through here, then the exit pill instead of a 6th milestone.
        Assert.Equal(3, trail.LastVisibleIndex);
    }

    [Fact]
    public void Exceptional_Approval_Rejected_Is_Same_Exit_Family_As_Plain_Reject()
    {
        var trail = WithdrawalHistoryTrailBuilder.Build(
            Array.Empty<WithdrawalRequestStatusHistoryViewModel>(),
            WithdrawalStatusNames.RejectExceptionalApproval);

        Assert.Equal("Rejected", trail.Exit?.Label);
    }

    [Fact]
    public void Recall_In_Progress_Shows_Recall_Exit_Label()
    {
        var t = DateTime.UtcNow;
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
            Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.PendingRTGSVerification, t.AddMinutes(1)),
            Entry(WithdrawalStatusNames.PendingRTGSVerification, WithdrawalStatusNames.RecallRequested, t.AddMinutes(2)),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.RecallVerified);

        Assert.Equal("Recalled", trail.Exit?.Label);
        Assert.Equal(WithdrawalStatusNames.RecallVerified, trail.Exit?.StatusName);
        // Reached through RTGS Code / Confirmation (milestone index 1) before the recall.
        Assert.True(trail.Milestones[1].IsReached);
        Assert.False(trail.Milestones[2].IsReached);
    }

    [Fact]
    public void Recall_Rejected_Resumes_Normal_Backbone_Position_Not_An_Exit()
    {
        // Per ADR-0001, WorkflowServices resolves RejectRecall straight back to whatever backbone
        // status the request was at before the recall, so "Recall Rejected" never actually appears
        // as a NewStatusName — the history just shows the request back at its prior status.
        var t = DateTime.UtcNow;
        var history = new[]
        {
            Entry(WithdrawalStatusNames.Draft, WithdrawalStatusNames.PendingInternalApproval, t),
            Entry(WithdrawalStatusNames.PendingInternalApproval, WithdrawalStatusNames.PendingRTGSVerification, t.AddMinutes(1)),
            Entry(WithdrawalStatusNames.PendingRTGSVerification, WithdrawalStatusNames.RecallRequested, t.AddMinutes(2)),
            Entry(WithdrawalStatusNames.RecallRequested, WithdrawalStatusNames.RecallVerified, t.AddMinutes(3)),
            Entry(WithdrawalStatusNames.RecallVerified, WithdrawalStatusNames.PendingRTGSVerification, t.AddMinutes(4)),
        };

        var trail = WithdrawalHistoryTrailBuilder.Build(history, WithdrawalStatusNames.PendingRTGSVerification);

        Assert.Null(trail.Exit);
        Assert.True(trail.Milestones[1].IsCurrent);
    }

    [Fact]
    public void Approved_Request_Reaches_Final_Milestone()
    {
        var trail = WithdrawalHistoryTrailBuilder.Build(
            new[] { Entry(WithdrawalStatusNames.PendingApproval, WithdrawalStatusNames.Approved, DateTime.UtcNow) },
            WithdrawalStatusNames.Approved);

        Assert.True(trail.Milestones[4].IsReached);
        Assert.True(trail.Milestones[4].IsCurrent);
        Assert.Null(trail.Exit);
    }

    [Fact]
    public void NextActor_And_IsClosed_Still_Reflect_Current_Status()
    {
        var trail = WithdrawalHistoryTrailBuilder.Build(
            Array.Empty<WithdrawalRequestStatusHistoryViewModel>(),
            WithdrawalStatusNames.PendingVerification);

        Assert.Equal("Panel B", trail.NextActor);
        Assert.False(trail.IsClosed);
    }

    [Fact]
    public void Terminal_Approved_Status_Has_No_Next_Actor_And_Is_Closed()
    {
        var trail = WithdrawalHistoryTrailBuilder.Build(
            Array.Empty<WithdrawalRequestStatusHistoryViewModel>(),
            WithdrawalStatusNames.Approved);

        Assert.Null(trail.NextActor);
        Assert.True(trail.IsClosed);
    }
}
