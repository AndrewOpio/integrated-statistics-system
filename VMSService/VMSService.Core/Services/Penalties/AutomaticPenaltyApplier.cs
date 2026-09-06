using Microsoft.Extensions.Logging;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Penalties;

namespace VMSService.Core.Services.Penalties
{
    /// <summary>
    /// Shared FindByName → check-not-already-applied → Apply flow for automatic (non-manual)
    /// penalties, used by every automatic-penalty trigger site regardless of which controller or
    /// service it lives in.
    /// </summary>
    public static class AutomaticPenaltyApplier
    {
        /// <summary>
        /// Applies <paramref name="penaltyName"/> against <paramref name="sourceId"/> unless it's
        /// already been recorded (including a soft-deleted/waived row — re-triggering the same
        /// event must never resurrect a penalty someone removed by hand). Swallows and logs any
        /// failure rather than letting it propagate, so a penalty-side problem never blocks the
        /// business action that triggered it. Returns whether it actually applied.
        /// </summary>
        public static async Task<bool> ApplyIfNotAlreadyAppliedAsync(
            IPenaltyRepository penaltyRepository,
            IAppliedPenaltyRepository appliedPenaltyRepository,
            ILogger logger,
            string source,
            string? sourceId,
            string penaltyName,
            string description)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) return false;

            try
            {
                var penalty = await penaltyRepository.FindByName(penaltyName);
                if (penalty is null)
                {
                    logger.LogWarning("Automatic penalty '{PenaltyName}' not found — skipping.", penaltyName);
                    return false;
                }

                if (await appliedPenaltyRepository.ExistsForSource(sourceId, penalty.Id)) return false;

                await appliedPenaltyRepository.Apply(source, sourceId, penalty.Id, description: description);
                await appliedPenaltyRepository.Save();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying automatic penalty '{PenaltyName}' for source {SourceId}", penaltyName, sourceId);
                return false;
            }
        }
    }
}
