using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PenaltyWaivers
{
    public interface IPenaltyWaiverRepository : IRepositoryBase<PenaltyWaiver, int>
    {
        Task<ICollection<PenaltyWaiver>> FindByPenalty(int penaltyId);

        /// <summary>True if an active waiver covers this penalty, branch, and date.</summary>
        Task<bool> IsWaivedAsync(int penaltyId, int bouBranchId, DateOnly onDate);

        /// <summary>The active waiver covering this penalty, branch, and date — if any, for display purposes (e.g. its end date).</summary>
        Task<PenaltyWaiver?> FindActiveAsync(int penaltyId, int bouBranchId, DateOnly onDate);
    }
}
