using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Models.WithdrawalRequests;
using VMSService.Core.Static;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Services.Withdrawals
{
    // Builds the BOU-facing stepper as a fixed 5-milestone backbone (WithdrawalMilestoneMap)
    // instead of one pill per StatusHistory row, so a request that has looped through Return any
    // number of times still renders the same handful of pills. See ticket 36.
    public static class WithdrawalHistoryTrailBuilder
    {
        public static WithdrawalHistoryTrailViewModel Build(
            IEnumerable<WithdrawalRequestStatusHistoryViewModel> statusHistory,
            string currentStatusName)
        {
            var history = statusHistory.OrderBy(h => h.ChangedAt).ToList();

            int reachedIndex = -1;
            foreach (var entry in history)
            {
                var milestone = WithdrawalMilestoneMap.GetMilestone(entry.NewStatusName);
                if (milestone is null) continue;

                var index = Array.IndexOf(WithdrawalMilestoneMap.Backbone, milestone);
                if (index > reachedIndex) reachedIndex = index;
            }

            var currentMilestone = WithdrawalMilestoneMap.GetMilestone(currentStatusName);
            bool isOnBackbone = currentMilestone is not null;
            if (isOnBackbone)
            {
                var index = Array.IndexOf(WithdrawalMilestoneMap.Backbone, currentMilestone);
                if (index > reachedIndex) reachedIndex = index;
            }

            // A request with no backbone history yet (still Internal Draft) is at milestone 0.
            if (reachedIndex < 0) reachedIndex = 0;

            var milestones = WithdrawalMilestoneMap.Backbone
                .Select((name, i) => new WithdrawalMilestoneStep
                {
                    Name = name,
                    IsReached = i <= reachedIndex,
                    IsCurrent = isOnBackbone && i == reachedIndex,
                })
                .ToList();

            WithdrawalTrailExit? exit = null;
            if (!isOnBackbone)
            {
                if (WithdrawalMilestoneMap.IsRejectExit(currentStatusName))
                    exit = new WithdrawalTrailExit { Label = "Rejected", StatusName = currentStatusName };
                else if (WithdrawalMilestoneMap.IsRecallExit(currentStatusName))
                    exit = new WithdrawalTrailExit { Label = "Recalled", StatusName = currentStatusName };
            }

            var returnCount = history.Count(h => h.NewStatusName == WithdrawalStatusNames.ReturnedForRevision);
            var nextActor = WithdrawalNextActorMap.GetNextActor(currentStatusName);

            return new WithdrawalHistoryTrailViewModel
            {
                Milestones = milestones,
                Exit = exit,
                ReturnCount = returnCount,
                NextActor = nextActor,
                IsClosed = nextActor is null,
            };
        }
    }
}
