using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MIandFits
{
    public interface IMIandFitRepository : IRepositoryBase<MIandFit, int>
    {
        Task<ICollection<MIandFit>> FindByPlanId(int planId);
        Task<ICollection<MIandFit>> FindByBoardSupervisor(string userName);
        Task<bool> SaveAll();
    }
}
