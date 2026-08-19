using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoardSessionSurplusShortageStatusHistories
{
    public interface IBoardSessionSurplusShortageStatusHistoryRepository : IRepositoryBase<BoardSessionSurplusShortageStatusHistory, int>
    {
        Task<ICollection<BoardSessionSurplusShortageStatusHistory>> FindByBoardSessionSurplusShortageId(int boardSessionSurplusShortageId);
    }
}
