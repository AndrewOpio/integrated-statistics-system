using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ReceivedStockStatusHistories
{
    public interface IReceivedStockStatusHistoryRepository : IRepositoryBase<ReceivedStockStatusHistory, int>
    {
        Task<ICollection<ReceivedStockStatusHistory>> FindByReceivedStockId(int receivedStockId);
    }
}