namespace VMSService.Core.Models.WithdrawalRequests
{
    public class WithdrawalHistoryTrailStep
    {
        public string StatusName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
    }

    // Drives the BOU-facing stepper. Built from the request's actual StatusHistory rather than
    // a fixed step list, since the workflow branches (exceptional approval, recall, reject) and
    // a fixed list can't represent "what actually happened" for every path at once.
    public class WithdrawalHistoryTrailViewModel
    {
        public List<WithdrawalHistoryTrailStep> Steps { get; set; } = new();

        /// <summary>Who owns the request right now — null if it's closed.</summary>
        public string? NextActor { get; set; }
        public bool IsClosed { get; set; }
    }
}
