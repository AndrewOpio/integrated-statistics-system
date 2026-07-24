using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Penalties
{
    public interface IPenaltyRepository : IRepositoryBase<Penalty, int>
    {
        Task<ICollection<Penalty>> FindByActive(bool active);

        /// <summary>Active penalties for a given AppliesTo category — e.g. the Pre-Transaction checklist for withdrawals.</summary>
        Task<ICollection<Penalty>> FindApplicableFor(string appliesTo);

        /// <summary>
        /// Computes the charge for a penalty from its configured rate field plus the values entered
        /// for this occurrence. Pure and stateless — no DB access, no rounding; round when persisting.
        /// </summary>
        decimal Compute(Penalty penalty, decimal quantity = 1, decimal denomValue = 0,
                         decimal baseValue = 0, decimal correctlyClassifiedValue = 0);
    }
}
