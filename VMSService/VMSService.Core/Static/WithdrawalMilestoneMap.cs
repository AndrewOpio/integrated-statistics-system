using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Static
{
    // Collapses the withdrawal workflow's many statuses onto a small, fixed backbone the BOU
    // stepper can render regardless of how many times a request has looped through Return —
    // the normal (RTGS Code) and Exceptional paths both converge on Verification, so both fit
    // the same 5 milestones. See ticket 36 and CONTEXT.md's Return/Recall/Reject glossary.
    public static class WithdrawalMilestoneMap
    {
        public const string CbApproval = "CB Approval";
        public const string RtgsCodeConfirmation = "RTGS Code / Confirmation";
        public const string Verification = "Verification";
        public const string ApprovalMilestone = "Approval";
        public const string ApprovedMilestone = "Approved";

        public static readonly string[] Backbone =
        [
            CbApproval,
            RtgsCodeConfirmation,
            Verification,
            ApprovalMilestone,
            ApprovedMilestone,
        ];

        private static readonly Dictionary<string, string> ByStatus = new()
        {
            [WithdrawalStatusNames.Draft] = CbApproval,
            [WithdrawalStatusNames.PendingInternalApproval] = CbApproval,
            [WithdrawalStatusNames.ReturnedForRevision] = CbApproval,
            [WithdrawalStatusNames.PendingRTGSVerification] = RtgsCodeConfirmation,
            [WithdrawalStatusNames.PendingWithdrawalConfirmation] = RtgsCodeConfirmation,
            [WithdrawalStatusNames.VerificationFailed] = RtgsCodeConfirmation,
            [WithdrawalStatusNames.PendingVerification] = Verification,
            [WithdrawalStatusNames.PendingExceptionalApproval] = Verification,
            [WithdrawalStatusNames.PendingExceptionalVerification] = Verification,
            [WithdrawalStatusNames.PendingApproval] = ApprovalMilestone,
            [WithdrawalStatusNames.Approved] = ApprovedMilestone,
        };

        // Reject and Exceptional-Reject are "the same family" per CONTEXT.md — both render as
        // the same exit label, distinguished only by the tooltip's actual status name.
        private static readonly HashSet<string> RejectExitStatuses =
        [
            WithdrawalStatusNames.Rejected,
            WithdrawalStatusNames.RejectComplete,
            WithdrawalStatusNames.RejectExceptionalApproval,
            WithdrawalStatusNames.RejectExceptionalApprovalComplete,
        ];

        // Recall Rejected is deliberately absent: WorkflowServices resolves RejectRecall straight
        // to whatever backbone status the request was at before the recall (ADR-0001), so it never
        // appears as a StatusHistory.NewStatusName — the request just resumes on the backbone.
        private static readonly HashSet<string> RecallExitStatuses =
        [
            WithdrawalStatusNames.RecallRequested,
            WithdrawalStatusNames.RecallVerified,
            WithdrawalStatusNames.RecallApproved,
            WithdrawalStatusNames.RecallComplete,
        ];

        /// <summary>Null if the status isn't one of the fixed backbone stages.</summary>
        public static string? GetMilestone(string statusName) => ByStatus.GetValueOrDefault(statusName);

        public static bool IsRejectExit(string statusName) => RejectExitStatuses.Contains(statusName);

        public static bool IsRecallExit(string statusName) => RecallExitStatuses.Contains(statusName);
    }
}
