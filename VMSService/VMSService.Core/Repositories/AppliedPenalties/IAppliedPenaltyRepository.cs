using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AppliedPenalties
{
    public interface IAppliedPenaltyRepository : IRepositoryBase<AppliedPenalty, int>
    {
        Task<ICollection<AppliedPenalty>> FindBySourceId(string sourceId);

        /// <summary>
        /// Loads the Penalty, computes the charge via IPenaltyRepository.Compute, and upserts by
        /// (sourceId, penaltyId) — updates the existing row if one exists, otherwise stages a new one.
        /// Stages only; call Save() to commit.
        /// </summary>
        Task<AppliedPenalty> Apply(string source, string sourceId, int penaltyId, int? quantity = null,
                                    decimal? denomValue = null, decimal? baseValue = null,
                                    decimal? correctlyClassifiedValue = null);

        /// <summary>Hard-removes a previously applied penalty (no status/soft-delete column on this entity). Stages only; call Save() to commit.</summary>
        Task Remove(int appliedPenaltyId);
    }
}
