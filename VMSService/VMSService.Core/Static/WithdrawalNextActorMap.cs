using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Static
{
    // Who owns a withdrawal request while it sits at a given status — lets the BOU-facing UI
    // tell a viewer who to chase without relying on AvailableTransitions, which is scoped to
    // the viewer's own roles and can't describe what someone else needs to do.
    public static class WithdrawalNextActorMap
    {
        private static readonly Dictionary<string, string> ByStatus = new()
        {
            [WithdrawalStatusNames.PendingInternalApproval] = "CB Approver",
            [WithdrawalStatusNames.ReturnedForRevision] = "CB Maker",
            [WithdrawalStatusNames.PendingRTGSVerification] = "CB (RTGS code entry)",
            [WithdrawalStatusNames.PendingWithdrawalConfirmation] = "Back Office",
            [WithdrawalStatusNames.PendingExceptionalApproval] = "Back Office",
            [WithdrawalStatusNames.PendingExceptionalVerification] = "Currency Approver",
            [WithdrawalStatusNames.PendingVerification] = "Panel B",
            [WithdrawalStatusNames.PendingApproval] = "Panel A",
            [WithdrawalStatusNames.RecallRequested] = "Back Office",
            [WithdrawalStatusNames.RecallVerified] = "Currency Approver",
            [WithdrawalStatusNames.RecallApproved] = "Back Office",
        };

        /// <summary>Null means the request is closed — no one needs to act on it next.</summary>
        public static string? GetNextActor(string statusName)
            => ByStatus.GetValueOrDefault(statusName);
    }
}
