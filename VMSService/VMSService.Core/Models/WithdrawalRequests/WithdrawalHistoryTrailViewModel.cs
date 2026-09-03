namespace VMSService.Core.Models.WithdrawalRequests
{
    public class WithdrawalMilestoneStep
    {
        public string Name { get; set; } = string.Empty;
        public bool IsReached { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>The request has left the backbone via Reject or Recall — <see cref="Label"/> replaces
    /// the remaining milestone pills in the view; <see cref="StatusName"/> is the actual current
    /// status, for the exit pill's tooltip (e.g. "Recall Verified").</summary>
    public class WithdrawalTrailExit
    {
        public string Label { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
    }

    // Drives the BOU-facing stepper. Fixed-size — 5 milestones from WithdrawalMilestoneMap — so a
    // request that has looped through Return any number of times still renders the same handful of
    // pills instead of one per StatusHistory row. See ticket 36.
    public class WithdrawalHistoryTrailViewModel
    {
        public List<WithdrawalMilestoneStep> Milestones { get; set; } = new();

        /// <summary>Null while the request is still on the backbone.</summary>
        public WithdrawalTrailExit? Exit { get; set; }

        /// <summary>Index of the last milestone pill the view should render — every reached
        /// milestone when there's no exit, otherwise the last one reached before exiting.</summary>
        public int LastVisibleIndex => Exit is null
            ? Milestones.Count - 1
            : Milestones.FindLastIndex(m => m.IsReached);

        /// <summary>How many times this request has been sent back via ReturnForRevision.</summary>
        public int ReturnCount { get; set; }

        /// <summary>Who owns the request right now — null if it's closed.</summary>
        public string? NextActor { get; set; }
        public bool IsClosed { get; set; }
    }
}
