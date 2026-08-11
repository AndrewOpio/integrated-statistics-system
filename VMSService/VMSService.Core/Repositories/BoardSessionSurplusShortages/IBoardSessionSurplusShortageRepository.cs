using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoardSessionSurplusShortages
{
    public interface IBoardSessionSurplusShortageRepository : IRepositoryBase<BoardSessionSurplusShortage, int>
    {
        Task<ICollection<BoardSessionSurplusShortage>> FindByBranchId(int branchId);
    }
}
