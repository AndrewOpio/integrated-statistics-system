using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CountingRoomSessions
{
    public interface ICountingRoomSessionRepository : IRepositoryBase<CountingRoomSession, int>
    {
        Task<ICollection<CountingRoomSession>> FindByStockProcessingPlanId(int stockProcessingPlanId);
    }
}
