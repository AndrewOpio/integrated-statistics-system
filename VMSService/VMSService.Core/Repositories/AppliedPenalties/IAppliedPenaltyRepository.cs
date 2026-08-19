using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AppliedPenalties
{
    public interface IAppliedPenaltyRepository : IRepositoryBase<AppliedPenalty, int>
    {
        Task<ICollection<AppliedPenalty>> FindBySourceId(string sourceId);

        /// <summary>All applied penalties not yet settled on RTGS, across every source (withdrawal, deposit, processing).</summary>
        Task<ICollection<AppliedPenalty>> FindUnsettled();

        /// <summary>
        /// Loads the Penalty, computes the charge via IPenaltyRepository.Compute, and upserts by
        /// (sourceId, penaltyId) — updates the existing row if one exists, otherwise stages a new one.
        /// A previously removed row is reused and un-deleted rather than duplicated.
        /// Stages only; call Save() to commit.
        /// </summary>
        /// <param name="description">
        /// Human-readable summary of the transaction the penalty was raised against. Description is
        /// a required column, so callers that pass null get an empty string on insert; on update a
        /// null leaves any existing description intact.
        /// </param>
        Task<AppliedPenalty> Apply(string source, string sourceId, int penaltyId, int? quantity = null,
                                    decimal? denomValue = null, decimal? baseValue = null,
                                    decimal? correctlyClassifiedValue = null, string? description = null);

        /// <summary>
        /// Soft-deletes a previously applied penalty by setting IsDeleted — the row is retained so the
        /// history of it having been applied survives. Stages only; call Save() to commit.
        /// </summary>
        Task Remove(int appliedPenaltyId);
    }
}
