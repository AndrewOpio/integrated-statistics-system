using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbBoxes
{
    public interface ICfbBoxRepository : IRepositoryBase<CfbBox, int>
    {
        Task<ICollection<CfbBox>> FindByBranchId(int bouBranchId);

        new Task<bool> Create(CfbBox entity);
    }
}
